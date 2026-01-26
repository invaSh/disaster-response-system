using Amazon.SQS;
using Amazon.SQS.Model;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Hosting;
using NotificationService.Services;

namespace NotificationService.Messaging.Consumers;

public class IncidentEventConsumer : BackgroundService
{
    private readonly IAmazonSQS _sqsClient;
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly ILogger<IncidentEventConsumer> _logger;
    private string? _queueUrl;

    public IncidentEventConsumer(
        IAmazonSQS sqsClient,
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        ILogger<IncidentEventConsumer> logger)
    {
        _sqsClient = sqsClient;
        _serviceProvider = serviceProvider;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await InitializeQueueAsync(stoppingToken);

        _logger.LogInformation("IncidentEventConsumer started. Polling queue: {QueueUrl}", _queueUrl);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var receiveRequest = new ReceiveMessageRequest
                {
                    QueueUrl = _queueUrl,
                    MaxNumberOfMessages = 10,
                    WaitTimeSeconds = 20, // long polling
                    MessageAttributeNames = new List<string> { "All" }
                };

                var response = await _sqsClient.ReceiveMessageAsync(receiveRequest, stoppingToken);

                if (response.Messages != null && response.Messages.Any())
                {
                    _logger.LogInformation("Received {Count} messages from queue", response.Messages.Count);

                    foreach (var message in response.Messages)
                    {
                        await ProcessMessageAsync(message, stoppingToken);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error receiving messages from SQS queue");
                await Task.Delay(5000, stoppingToken);
            }
        }
    }

    private async Task InitializeQueueAsync(CancellationToken cancellationToken)
    {
        var queueName = _configuration["AWS:SQS:QueueName"] ?? "notification-incident-queue";
        
        try
        {
            var queueUrlResponse = await _sqsClient.GetQueueUrlAsync(queueName, cancellationToken);
            _queueUrl = queueUrlResponse.QueueUrl;
            _logger.LogInformation("Initialized queue URL: {QueueUrl}", _queueUrl);
        }
        catch (QueueDoesNotExistException)
        {
            _logger.LogWarning("Queue {QueueName} does not exist. Will retry...", queueName);
            await Task.Delay(5000, cancellationToken);
            await InitializeQueueAsync(cancellationToken);
        }
    }

    private async Task ProcessMessageAsync(Message message, CancellationToken cancellationToken)
    {
        try
        {
            var snsMessage = JsonSerializer.Deserialize<SNSMessage>(message.Body, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (snsMessage == null || string.IsNullOrEmpty(snsMessage.Message))
            {
                _logger.LogWarning("Invalid SNS message format. Deleting message.");
                await DeleteMessageAsync(message.ReceiptHandle);
                return;
            }

            var incidentEvent = JsonSerializer.Deserialize<IncidentCreatedEvent>(snsMessage.Message, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (incidentEvent == null || incidentEvent.EventType != "IncidentCreated")
            {
                _logger.LogWarning("Invalid event type or format. Deleting message.");
                await DeleteMessageAsync(message.ReceiptHandle);
                return;
            }

            _logger.LogInformation("Processing IncidentCreated event for incident {IncidentId}", incidentEvent.Data?.ID);

            // process eventin
            using var scope = _serviceProvider.CreateScope();
            var notificationSvc = scope.ServiceProvider.GetRequiredService<NotificationSvc>();

            if (incidentEvent.Data?.CreatedByUserId is Guid createdByUserId)
            {
                await notificationSvc.CreateIncidentCreatedNotification(
                    createdByUserId: createdByUserId,
                    incidentDbId: incidentEvent.Data.ID,
                    incidentPublicId: incidentEvent.Data.IncidentId,
                    incidentStatus: incidentEvent.Data.Status,
                    severity: incidentEvent.Data.Severity,
                    latitude: incidentEvent.Data.Latitude,
                    longitude: incidentEvent.Data.Longitude,
                    ct: cancellationToken);

                _logger.LogInformation("Created notification for incident {IncidentId}", incidentEvent.Data.ID);
            }

            await DeleteMessageAsync(message.ReceiptHandle);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message. Message will be retried or moved to DLQ.");
            
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
            _logger.LogError(ex, "Error deleting message from queue");
        }
    }

    // Helper classes per deserializim
    private class SNSMessage
    {
        public string Type { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string TopicArn { get; set; } = string.Empty;
    }

    private class IncidentCreatedEvent
    {
        public string EventType { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public IncidentData? Data { get; set; }
    }

    private class IncidentData
    {
        public string ID { get; set; } = string.Empty;
        public string IncidentId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public Guid? CreatedByUserId { get; set; }
    }
}
