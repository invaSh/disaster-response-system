using Amazon.SQS;
using Amazon.SQS.Model;
using System.Text.Json;
using DispatchService.Services;
using DispatchService.DTOs.DispatchOrders;
using Microsoft.Extensions.Hosting;

namespace DispatchService.Messaging.Consumers;

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
        var queueName = _configuration["AWS:SQS:UpdatedQueueName"] ?? "dispatch-incident-updated-queue";
        
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

            _logger.LogInformation("Processing IncidentUpdated event for incident {IncidentId}. Event data: Status={Status}, Severity={Severity}, Title={Title}, Location=({Lat},{Lng})", 
                incidentEvent.Data?.ID, 
                incidentEvent.Data?.Status ?? "null",
                incidentEvent.Data?.Severity ?? "null",
                incidentEvent.Data?.Title ?? "null",
                incidentEvent.Data?.Latitude ?? 0,
                incidentEvent.Data?.Longitude ?? 0);

            if (incidentEvent.Data == null || !Guid.TryParse(incidentEvent.Data.ID, out var incidentId))
            {
                _logger.LogWarning("Invalid incident ID in event data: {IncidentId}", incidentEvent.Data?.ID);
                await DeleteMessageAsync(message.ReceiptHandle);
                return;
            }

            // update cached incident dhe dispatch order
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<DispatchService.Persistance.DispatchDbContext>();
            var dispatchService = scope.ServiceProvider.GetRequiredService<DispatchSvc>();

            // krahasimi i vlerave tvjetra me kto t'rejat qe po dojne me u bo update
            var cachedIncident = await dbContext.Incidents.FindAsync(new object[] { incidentId }, cancellationToken);
            
            var oldStatus = cachedIncident?.Status ?? string.Empty;
            var oldSeverity = cachedIncident?.Severity ?? string.Empty;
            var oldTitle = cachedIncident?.Title ?? string.Empty;
            var oldLatitude = cachedIncident?.Latitude ?? 0;
            var oldLongitude = cachedIncident?.Longitude ?? 0;

            if (cachedIncident != null)
            {
                if (!string.IsNullOrEmpty(incidentEvent.Data.Title))
                    cachedIncident.Title = incidentEvent.Data.Title;
                
                if (!string.IsNullOrEmpty(incidentEvent.Data.Type))
                    cachedIncident.Type = incidentEvent.Data.Type;
                
                if (!string.IsNullOrEmpty(incidentEvent.Data.Severity))
                    cachedIncident.Severity = incidentEvent.Data.Severity;
                
                if (!string.IsNullOrEmpty(incidentEvent.Data.Status))
                    cachedIncident.Status = incidentEvent.Data.Status;
                
                if (incidentEvent.Data.Latitude != 0)
                    cachedIncident.Latitude = incidentEvent.Data.Latitude;
                
                if (incidentEvent.Data.Longitude != 0)
                    cachedIncident.Longitude = incidentEvent.Data.Longitude;
                
                cachedIncident.LastSyncedAt = DateTime.UtcNow;
                
                await dbContext.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Updated cached incident {IncidentId}", incidentId);
            }

            try
            {
                var dispatchOrder = await dispatchService.GetDispatchOrderByIncidentId(incidentId, cancellationToken);
                
                var notesUpdates = new List<string>();
                var hasChanges = false;

                if (!string.IsNullOrEmpty(incidentEvent.Data.Status) && 
                    !incidentEvent.Data.Status.Equals(oldStatus, StringComparison.OrdinalIgnoreCase))
                {
                    var status = incidentEvent.Data.Status;
                    notesUpdates.Add($"Incident status updated to: {status}");
                    hasChanges = true;
                    
                    if (status.Equals("Resolved", StringComparison.OrdinalIgnoreCase) || 
                        status.Equals("Closed", StringComparison.OrdinalIgnoreCase))
                    {
                        notesUpdates.Add($"Incident has been {status.ToLower()}. Dispatch order may need review.");
                    }
                }

                if (incidentEvent.Data.Latitude != 0 && incidentEvent.Data.Longitude != 0 &&
                    (Math.Abs(incidentEvent.Data.Latitude - oldLatitude) > 0.0001 || 
                     Math.Abs(incidentEvent.Data.Longitude - oldLongitude) > 0.0001))
                {
                    notesUpdates.Add($"Incident location updated: Lat {incidentEvent.Data.Latitude}, Long {incidentEvent.Data.Longitude}");
                    hasChanges = true;
                }

                if (!string.IsNullOrEmpty(incidentEvent.Data.Severity) && 
                    !incidentEvent.Data.Severity.Equals(oldSeverity, StringComparison.OrdinalIgnoreCase))
                {
                    notesUpdates.Add($"Incident severity updated to: {incidentEvent.Data.Severity}");
                    hasChanges = true;
                }

                if (!string.IsNullOrEmpty(incidentEvent.Data.Title) && 
                    !incidentEvent.Data.Title.Equals(oldTitle, StringComparison.OrdinalIgnoreCase))
                {
                    notesUpdates.Add($"Incident title updated: {incidentEvent.Data.Title}");
                    hasChanges = true;
                }

                if (hasChanges && notesUpdates.Any())
                {
                    var updateDto = new UpdateDispatchOrderRequestDTO
                    {
                        Notes = notesUpdates
                    };

                    await dispatchService.UpdateDispatchOrderNotes(dispatchOrder.Id, updateDto, cancellationToken);
                    _logger.LogInformation("Updated dispatch order {OrderId} for incident {IncidentId} with {Count} new notes", 
                        dispatchOrder.Id, incidentId, notesUpdates.Count);
                }
                else
                {
                    _logger.LogInformation("No relevant changes detected for dispatch order of incident {IncidentId} (values unchanged)", incidentId);
                }
            }
            catch (Exception ex) when (ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation("No dispatch order found for incident {IncidentId} - will be created on incident creation", incidentId);
            }

            _logger.LogInformation("Processed IncidentUpdated event for incident {IncidentId}", incidentId);

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
        public string Status { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }
}
