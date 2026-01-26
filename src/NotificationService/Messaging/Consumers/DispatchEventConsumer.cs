using Amazon.SQS;
using Amazon.SQS.Model;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using NotificationService.Services;
namespace NotificationService.Messaging.Consumers;

public class DispatchEventConsumer : BackgroundService
{
    private readonly IAmazonSQS _sqsClient;
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DispatchEventConsumer> _logger;
    private string? _queueUrl;

    public DispatchEventConsumer(
        IAmazonSQS sqsClient,
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        ILogger<DispatchEventConsumer> logger)
    {
        _sqsClient = sqsClient;
        _serviceProvider = serviceProvider;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await InitializeQueueAsync(stoppingToken);

        _logger.LogInformation("DispatchEventConsumer started. Polling queue: {QueueUrl}", _queueUrl);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var receiveRequest = new ReceiveMessageRequest
                {
                    QueueUrl = _queueUrl,
                    MaxNumberOfMessages = 10,
                    WaitTimeSeconds = 20,
                    MessageAttributeNames = new List<string> { "All" }
                };

                var response = await _sqsClient.ReceiveMessageAsync(receiveRequest, stoppingToken);

                if (response.Messages != null && response.Messages.Any())
                {
                    _logger.LogInformation("Received {Count} dispatch messages from queue", response.Messages.Count);

                    foreach (var msg in response.Messages)
                    {
                        await ProcessMessageAsync(msg, stoppingToken);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error receiving messages from dispatch SQS queue");
                await Task.Delay(5000, stoppingToken);
            }
        }
    }

    private async Task InitializeQueueAsync(CancellationToken cancellationToken)
    {
        var queueName = _configuration["AWS:SQS:DispatchEventsQueueName"] ?? "notification-dispatch-queue";

        try
        {
            var queueUrlResponse = await _sqsClient.GetQueueUrlAsync(queueName, cancellationToken);
            _queueUrl = queueUrlResponse.QueueUrl;
            _logger.LogInformation("Initialized dispatch queue URL: {QueueUrl}", _queueUrl);
        }
        catch (QueueDoesNotExistException)
        {
            _logger.LogWarning("Dispatch queue {QueueName} does not exist. Will retry...", queueName);
            await Task.Delay(5000, cancellationToken);
            await InitializeQueueAsync(cancellationToken);
        }
    }

    private async Task ProcessMessageAsync(Message msg, CancellationToken cancellationToken)
    {
        try
        {
            var snsMessage = JsonSerializer.Deserialize<SNSMessage>(msg.Body, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (snsMessage == null || string.IsNullOrEmpty(snsMessage.Message))
            {
                _logger.LogWarning("Invalid SNS message format. Deleting message.");
                await DeleteMessageAsync(msg.ReceiptHandle);
                return;
            }

            var dispatchEvent = JsonSerializer.Deserialize<DispatchEvent>(snsMessage.Message, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (dispatchEvent == null || string.IsNullOrWhiteSpace(dispatchEvent.EventType))
            {
                _logger.LogWarning("Invalid dispatch event format. Deleting message.");
                await DeleteMessageAsync(msg.ReceiptHandle);
                return;
            }

            if (!Guid.TryParse(dispatchEvent.Data?.IncidentId, out var incidentId))
            {
                _logger.LogWarning("DispatchOrderCreated missing/invalid IncidentId. Deleting message.");
                await DeleteMessageAsync(msg.ReceiptHandle);
                return;
            }

            if (!Guid.TryParse(dispatchEvent.Data?.CreatedByUserId, out var createdByUserId))
            {
                _logger.LogWarning("Dispatch event missing/invalid CreatedByUserId. Deleting message.");
                await DeleteMessageAsync(msg.ReceiptHandle);
                return;
            }

            Guid? dispatchOrderId = null;
            if (Guid.TryParse(dispatchEvent.Data?.DispatchOrderId, out var parsedOrderId))
                dispatchOrderId = parsedOrderId;

            Guid? dispatchAssignmentId = null;
            if (Guid.TryParse(dispatchEvent.Data?.DispatchAssignmentId, out var parsedAssignmentId))
                dispatchAssignmentId = parsedAssignmentId;

            using var scope = _serviceProvider.CreateScope();
            var notificationSvc = scope.ServiceProvider.GetRequiredService<NotificationSvc>();

            if (dispatchEvent.EventType is not ("DispatchOrderCreated" or "DispatchAssignmentCreated" or "DispatchAssignmentCompleted" or "DispatchOrderCompleted"))
            {
                await DeleteMessageAsync(msg.ReceiptHandle);
                return;
            }

            // M'iu ik duplicates
            if (dispatchEvent.EventType == "DispatchOrderCompleted")
            {
                _logger.LogInformation("Skipping DispatchOrderCompleted notification to avoid duplicates.");
                await DeleteMessageAsync(msg.ReceiptHandle);
                return;
            }

            if (dispatchEvent.EventType == "DispatchAssignmentCreated")
            {
                var statusStr = dispatchEvent.Data?.AssignmentStatus ?? string.Empty;
                if (statusStr != "1")
                {
                    _logger.LogInformation("Skipping DispatchAssignmentCreated notification because status != 1 (status={Status})", statusStr);
                    await DeleteMessageAsync(msg.ReceiptHandle);
                    return;
                }
            }

            if (dispatchEvent.EventType == "DispatchAssignmentCompleted")
            {
                var statusStr = dispatchEvent.Data?.AssignmentStatus ?? string.Empty;
                if (statusStr != "4")
                {
                    _logger.LogInformation("Skipping DispatchAssignmentCompleted notification because status != 4 (status={Status})", statusStr);
                    await DeleteMessageAsync(msg.ReceiptHandle);
                    return;
                }
            }

            await notificationSvc.CreateDispatchNotification(
                createdByUserId: createdByUserId,
                incidentId: incidentId,
                dispatchEventType: dispatchEvent.EventType,
                dispatchOrderId: dispatchOrderId,
                dispatchAssignmentId: dispatchAssignmentId,
                ct: cancellationToken);

            _logger.LogInformation("Processed {EventType}. Incident {IncidentId}, Order {DispatchOrderId}, Assignment {DispatchAssignmentId}",
                dispatchEvent.EventType, incidentId, dispatchOrderId, dispatchAssignmentId);

            await DeleteMessageAsync(msg.ReceiptHandle);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing dispatch event message. Message will be retried or moved to DLQ.");
        }
    }

    private async Task DeleteMessageAsync(string receiptHandle)
    {
        try
        {
            await _sqsClient.DeleteMessageAsync(_queueUrl, receiptHandle);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting message from dispatch queue");
        }
    }

    private class SNSMessage
    {
        public string Type { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string TopicArn { get; set; } = string.Empty;
    }

    private class DispatchEvent
    {
        public string EventType { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public DispatchEventData? Data { get; set; }
    }

    private class DispatchEventData
    {
        public string DispatchOrderId { get; set; } = string.Empty;
        public string DispatchAssignmentId { get; set; } = string.Empty;
        public string IncidentId { get; set; } = string.Empty;
        public string CreatedByUserId { get; set; } = string.Empty;
        public string AssignmentStatus { get; set; } = string.Empty;
    }
}

