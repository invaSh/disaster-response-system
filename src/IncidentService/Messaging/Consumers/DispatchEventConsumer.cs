using Amazon.SQS;
using Amazon.SQS.Model;
using System.Text.Json;
using IncidentService.Services;
using IncidentService.Enums;
using Microsoft.Extensions.Hosting;
using MediatR;
using IncidentService.Application.Incident;

namespace IncidentService.Messaging.Consumers;

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
        var queueName = _configuration["AWS:SQS:DispatchQueueName"] ?? "incident-dispatch-queue";
        
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
            // SNS messages are wrapped in SQS messages
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

            var dispatchEvent = JsonSerializer.Deserialize<DispatchEvent>(snsMessage.Message, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (dispatchEvent == null || string.IsNullOrEmpty(dispatchEvent.EventType))
            {
                _logger.LogWarning("Invalid event type or format. Deleting message.");
                await DeleteMessageAsync(message.ReceiptHandle);
                return;
            }

            _logger.LogInformation("Processing {EventType} event for incident {IncidentId}", 
                dispatchEvent.EventType, dispatchEvent.Data?.IncidentId);

            // Process the event
            using var scope = _serviceProvider.CreateScope();
            var incidentService = scope.ServiceProvider.GetRequiredService<IncidentSvc>();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            if (dispatchEvent.Data == null || !Guid.TryParse(dispatchEvent.Data.IncidentId, out var incidentId))
            {
                _logger.LogWarning("Invalid incident ID in event data: {IncidentId}", dispatchEvent.Data?.IncidentId);
                await DeleteMessageAsync(message.ReceiptHandle);
                return;
            }

            // update statusin e incidentit bazuar ne events:
            Status newStatus = dispatchEvent.EventType switch
            {
                "DispatchOrderCreated" => Status.Acknowledged,
                "DispatchAssignmentCreated" => Status.InProgress,
                "DispatchAssignmentCompleted" => Status.Resolved,
                "DispatchOrderCompleted" => Status.Resolved,
                _ => throw new InvalidOperationException($"Unknown event type: {dispatchEvent.EventType}")
            };

            var incident = await incidentService.GetIncidentById(incidentId);

            var updatedAssignedUnits = incident.AssignedUnits?.ToList() ?? new List<Guid>();
            if (dispatchEvent.EventType == "DispatchAssignmentCreated")
            {
                if (Guid.TryParse(dispatchEvent.Data.UnitId, out var unitId))
                {
                    if (!updatedAssignedUnits.Contains(unitId))
                        updatedAssignedUnits.Add(unitId);
                }
                else
                {
                    _logger.LogWarning("DispatchAssignmentCreated missing/invalid UnitId. AssignedUnits will not be updated.");
                }
            }
            
            // update status me MediatR
            var updateCommand = new Update.Command
            {
                ID = incidentId,
                Title = incident.Title,
                Description = incident.Description ?? "No description", 
                Type = incident.Type.ToString(),
                ReporterName = incident.ReporterName,
                ReporterContact = incident.ReporterContact,
                Latitude = incident.Latitude,
                Longitude = incident.Longitude,
                Severity = incident.Severity.ToString(),
                Status = newStatus.ToString(),
                AssignedUnits = updatedAssignedUnits,
                ResolvedAt = dispatchEvent.EventType == "DispatchAssignmentCompleted"
                    ? (dispatchEvent.Timestamp == default ? DateTime.UtcNow : dispatchEvent.Timestamp)
                    : incident.ResolvedAt,
                Updates = null,
                Metadata = incident.Metadata
            };

            await mediator.Send(updateCommand, cancellationToken);
            
            _logger.LogInformation("Updated incident {IncidentId} status to {Status} based on {EventType}. IncidentUpdated event published.", 
                incidentId, newStatus, dispatchEvent.EventType);

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
        public string UnitId { get; set; } = string.Empty;
    }
}
