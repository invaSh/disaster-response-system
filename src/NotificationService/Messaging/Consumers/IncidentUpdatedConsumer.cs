using Amazon.SQS;
using Amazon.SQS.Model;
using System.Text.Json;
using NotificationService.Application.Notifications;
using MediatR;
using Microsoft.Extensions.Hosting;

namespace NotificationService.Messaging.Consumers;

public class IncidentUpdatedConsumer : BackgroundService
{
    private readonly IAmazonSQS _sqsClient;
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly ILogger<IncidentUpdatedConsumer> _logger;
    private string? _queueUrl;

    public IncidentUpdatedConsumer(
        IAmazonSQS sqsClient,
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        ILogger<IncidentUpdatedConsumer> logger)
    {
        _sqsClient = sqsClient;
        _serviceProvider = serviceProvider;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await InitializeQueueAsync(stoppingToken);

        _logger.LogInformation("IncidentUpdatedConsumer started. Polling queue: {QueueUrl}", _queueUrl);

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
        var queueName = _configuration["AWS:SQS:UpdatedQueueName"] ?? "notification-incident-updated-queue";
        
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

            var incidentEvent = JsonSerializer.Deserialize<IncidentUpdatedEvent>(snsMessage.Message, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (incidentEvent == null || incidentEvent.EventType != "IncidentUpdated")
            {
                _logger.LogWarning("Invalid event type or format. Deleting message.");
                await DeleteMessageAsync(message.ReceiptHandle);
                return;
            }

            _logger.LogInformation("Processing IncidentUpdated event for incident {IncidentId}", incidentEvent.Data?.ID);

            using var scope = _serviceProvider.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            var createNotificationCommand = new Create.Command
            {
                Title = $"Incident Updated: {incidentEvent.Data!.Title}",
                Message = $"The incident {incidentEvent.Data.IncidentId} has been updated.",
                Category = "Incident",
                Type = "Update",
                Severity = incidentEvent.Data.Severity,
                RecipientType = "System",
                RecipientId = "all",
                ReferenceType = "Incident",
                ReferenceId = incidentEvent.Data.ID,
                Metadata = new Dictionary<string, string>
                {
                    { "IncidentId", incidentEvent.Data.IncidentId },
                    { "EventType", "IncidentUpdated" }
                }
            };

            await mediator.Send(createNotificationCommand, cancellationToken);
            _logger.LogInformation("Created notification for incident update {IncidentId}", incidentEvent.Data.ID);

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

    private class SNSMessage
    {
        public string Type { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string TopicArn { get; set; } = string.Empty;
    }

    private class IncidentUpdatedEvent
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
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }
}
