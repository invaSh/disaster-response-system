using Amazon.SQS;
using Amazon.SQS.Model;
using System.Text.Json;
using DispatchService.Services;
using DispatchService.DTOs.DispatchOrders;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Hosting;

namespace DispatchService.Messaging.Consumers;

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
                await Task.Delay(5000, stoppingToken); // prit pak para se me retry
            }
        }
    }

    private async Task InitializeQueueAsync(CancellationToken cancellationToken)
    {
        var queueName = _configuration["AWS:SQS:QueueName"] ?? "dispatch-incident-queue";
        
        try
        {
            var queueUrlResponse = await _sqsClient.GetQueueUrlAsync(queueName, cancellationToken);
            _queueUrl = queueUrlResponse.QueueUrl;
            _logger.LogInformation("Initialized queue URL: {QueueUrl}", _queueUrl);
        }
        catch (QueueDoesNotExistException)
        {
            _logger.LogWarning("Queue {QueueName} does not exist. Will retry...", queueName);
            //  queue krijohet nga setup script, kshtu qe bojme retry
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

            if (incidentEvent.Data == null || !Guid.TryParse(incidentEvent.Data.ID, out var incidentId))
            {
                _logger.LogWarning("Invalid incident data in event");
                await DeleteMessageAsync(message.ReceiptHandle);
                return;
            }

            // cache the incident data localisht
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<DispatchService.Persistance.DispatchDbContext>();

            var existingIncident = await dbContext.Incidents.FindAsync(new object[] { incidentId }, cancellationToken);

            if (existingIncident == null)
            {
                var incident = new DispatchService.Domain.Incident
                {
                    Id = incidentId,
                    IncidentId = incidentEvent.Data.IncidentId,
                    Title = incidentEvent.Data.Title,
                    Type = incidentEvent.Data.Type,
                    Severity = incidentEvent.Data.Severity,
                    // match IncidentService Status enum, normal n'qat faze e ka "open"
                    Status = incidentEvent.Data.Status ?? "Open",
                    Latitude = incidentEvent.Data.Latitude,
                    Longitude = incidentEvent.Data.Longitude,
                    ReportedAt = DateTime.UtcNow,
                    LastSyncedAt = DateTime.UtcNow,
                    CreatedByUserId = incidentEvent.Data.CreatedByUserId
                };

                dbContext.Incidents.Add(incident);
                await dbContext.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Cached incident {IncidentId} ({Title}) locally", incidentId, incidentEvent.Data.Title);
            }
            else
            {
                _logger.LogInformation("Incident {IncidentId} already exists in cache", incidentId);
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
        public string? Status { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public Guid? CreatedByUserId { get; set; }
    }
}
