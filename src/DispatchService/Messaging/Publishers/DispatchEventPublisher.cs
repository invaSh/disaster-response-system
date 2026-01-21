using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using System.Text.Json;
using DispatchService.DTOs.DispatchOrders;
using DispatchService.DTOs.DispatchAssignments;

namespace DispatchService.Messaging.Publishers;

public interface IDispatchEventPublisher
{
    Task PublishDispatchOrderCreatedAsync(Guid dispatchOrderId, Guid incidentId);
    Task PublishDispatchAssignmentCreatedAsync(Guid dispatchAssignmentId, Guid dispatchOrderId, Guid incidentId);
    Task PublishDispatchOrderCompletedAsync(Guid dispatchOrderId, Guid incidentId);
}

public class DispatchEventPublisher : IDispatchEventPublisher
{
    private readonly IAmazonSimpleNotificationService _snsClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DispatchEventPublisher> _logger;

    public DispatchEventPublisher(
        IAmazonSimpleNotificationService snsClient,
        IConfiguration configuration,
        ILogger<DispatchEventPublisher> logger)
    {
        _snsClient = snsClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task PublishDispatchOrderCreatedAsync(Guid dispatchOrderId, Guid incidentId)
    {
        try
        {
            var topicName = _configuration["AWS:SNS:DispatchEventsTopic"] ?? "dispatch-events-topic";
            
            var topicsResponse = await _snsClient.ListTopicsAsync();
            var topicArn = topicsResponse.Topics
                .FirstOrDefault(t => t.TopicArn.Contains(topicName))?.TopicArn;

            if (topicArn == null)
            {
                _logger.LogWarning("SNS topic '{TopicName}' not found. Attempting to create it.", topicName);
                var createTopicResponse = await _snsClient.CreateTopicAsync(new CreateTopicRequest
                {
                    Name = topicName
                });
                topicArn = createTopicResponse.TopicArn;
            }

            var eventMessage = new
            {
                EventType = "DispatchOrderCreated",
                Timestamp = DateTime.UtcNow,
                Data = new
                {
                    DispatchOrderId = dispatchOrderId.ToString(),
                    IncidentId = incidentId.ToString()
                }
            };

            var messageBody = JsonSerializer.Serialize(eventMessage, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            var publishRequest = new PublishRequest
            {
                TopicArn = topicArn,
                Message = messageBody,
                MessageAttributes = new Dictionary<string, MessageAttributeValue>
                {
                    {
                        "EventType",
                        new MessageAttributeValue
                        {
                            DataType = "String",
                            StringValue = "DispatchOrderCreated"
                        }
                    }
                }
            };

            await _snsClient.PublishAsync(publishRequest);
            _logger.LogInformation("Published DispatchOrderCreated event for dispatch order {DispatchOrderId}, incident {IncidentId}", 
                dispatchOrderId, incidentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish DispatchOrderCreated event for dispatch order {DispatchOrderId}", dispatchOrderId);
        }
    }

    public async Task PublishDispatchAssignmentCreatedAsync(Guid dispatchAssignmentId, Guid dispatchOrderId, Guid incidentId)
    {
        try
        {
            var topicName = _configuration["AWS:SNS:DispatchEventsTopic"] ?? "dispatch-events-topic";
            
            var topicsResponse = await _snsClient.ListTopicsAsync();
            var topicArn = topicsResponse.Topics
                .FirstOrDefault(t => t.TopicArn.Contains(topicName))?.TopicArn;

            if (topicArn == null)
            {
                _logger.LogWarning("SNS topic '{TopicName}' not found. Attempting to create it.", topicName);
                var createTopicResponse = await _snsClient.CreateTopicAsync(new CreateTopicRequest
                {
                    Name = topicName
                });
                topicArn = createTopicResponse.TopicArn;
            }

            var eventMessage = new
            {
                EventType = "DispatchAssignmentCreated",
                Timestamp = DateTime.UtcNow,
                Data = new
                {
                    DispatchAssignmentId = dispatchAssignmentId.ToString(),
                    DispatchOrderId = dispatchOrderId.ToString(),
                    IncidentId = incidentId.ToString()
                }
            };

            var messageBody = JsonSerializer.Serialize(eventMessage, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            var publishRequest = new PublishRequest
            {
                TopicArn = topicArn,
                Message = messageBody,
                MessageAttributes = new Dictionary<string, MessageAttributeValue>
                {
                    {
                        "EventType",
                        new MessageAttributeValue
                        {
                            DataType = "String",
                            StringValue = "DispatchAssignmentCreated"
                        }
                    }
                }
            };

            await _snsClient.PublishAsync(publishRequest);
            _logger.LogInformation("Published DispatchAssignmentCreated event for assignment {DispatchAssignmentId}, dispatch order {DispatchOrderId}, incident {IncidentId}", 
                dispatchAssignmentId, dispatchOrderId, incidentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish DispatchAssignmentCreated event for assignment {DispatchAssignmentId}", dispatchAssignmentId);
        }
    }

    public async Task PublishDispatchOrderCompletedAsync(Guid dispatchOrderId, Guid incidentId)
    {
        try
        {
            var topicName = _configuration["AWS:SNS:DispatchEventsTopic"] ?? "dispatch-events-topic";
            
            var topicsResponse = await _snsClient.ListTopicsAsync();
            var topicArn = topicsResponse.Topics
                .FirstOrDefault(t => t.TopicArn.Contains(topicName))?.TopicArn;

            if (topicArn == null)
            {
                _logger.LogWarning("SNS topic '{TopicName}' not found. Attempting to create it.", topicName);
                var createTopicResponse = await _snsClient.CreateTopicAsync(new CreateTopicRequest
                {
                    Name = topicName
                });
                topicArn = createTopicResponse.TopicArn;
            }

            var eventMessage = new
            {
                EventType = "DispatchOrderCompleted",
                Timestamp = DateTime.UtcNow,
                Data = new
                {
                    DispatchOrderId = dispatchOrderId.ToString(),
                    IncidentId = incidentId.ToString()
                }
            };

            var messageBody = JsonSerializer.Serialize(eventMessage, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            var publishRequest = new PublishRequest
            {
                TopicArn = topicArn,
                Message = messageBody,
                MessageAttributes = new Dictionary<string, MessageAttributeValue>
                {
                    {
                        "EventType",
                        new MessageAttributeValue
                        {
                            DataType = "String",
                            StringValue = "DispatchOrderCompleted"
                        }
                    }
                }
            };

            await _snsClient.PublishAsync(publishRequest);
            _logger.LogInformation("Published DispatchOrderCompleted event for dispatch order {DispatchOrderId}, incident {IncidentId}", 
                dispatchOrderId, incidentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish DispatchOrderCompleted event for dispatch order {DispatchOrderId}", dispatchOrderId);
        }
    }
}
