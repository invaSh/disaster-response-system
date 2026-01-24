using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using System.Text.Json;
using IncidentService.DTOs;

namespace IncidentService.Messaging.Publishers;

public interface IIncidentEventPublisher
{
    Task PublishIncidentCreatedAsync(IncidentDTO incident);
    Task PublishIncidentUpdatedAsync(IncidentDTO incident);
}

public class IncidentEventPublisher : IIncidentEventPublisher
{
    private readonly IAmazonSimpleNotificationService _snsClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<IncidentEventPublisher> _logger;

    public IncidentEventPublisher(
        IAmazonSimpleNotificationService snsClient,
        IConfiguration configuration,
        ILogger<IncidentEventPublisher> logger)
    {
        _snsClient = snsClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task PublishIncidentCreatedAsync(IncidentDTO incident)
    {
        try
        {
            var topicName = _configuration["AWS:SNS:IncidentCreatedTopic"] ?? "incident-created-topic";
            
            // Get topic ARN by listing topics and finding the one that matches
            var topicsResponse = await _snsClient.ListTopicsAsync();
            var topicArn = topicsResponse.Topics
                .FirstOrDefault(t => t.TopicArn.Contains(topicName))?.TopicArn;

            if (topicArn == null)
            {
                _logger.LogWarning("SNS topic '{TopicName}' not found. Attempting to create it.", topicName);
                // Try to create the topic if it doesn't exist
                var createTopicResponse = await _snsClient.CreateTopicAsync(new CreateTopicRequest
                {
                    Name = topicName
                });
                topicArn = createTopicResponse.TopicArn;
            }

            var eventMessage = new
            {
                EventType = "IncidentCreated",
                Timestamp = DateTime.UtcNow,
                Data = incident
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
                            StringValue = "IncidentCreated"
                        }
                    }
                }
            };

            await _snsClient.PublishAsync(publishRequest);
            _logger.LogInformation("Published IncidentCreated event for incident {IncidentId}", incident.ID);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish IncidentCreated event for incident {IncidentId}", incident.ID);
        }
    }

    public async Task PublishIncidentUpdatedAsync(IncidentDTO incident)
    {
        try
        {
            var topicName = _configuration["AWS:SNS:IncidentUpdatedTopic"] ?? "incident-updated-topic";
            
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
                EventType = "IncidentUpdated",
                Timestamp = DateTime.UtcNow,
                Data = incident
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
                            StringValue = "IncidentUpdated"
                        }
                    }
                }
            };

            await _snsClient.PublishAsync(publishRequest);
            _logger.LogInformation("Published IncidentUpdated event for incident {IncidentId}", incident.ID);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish IncidentUpdated event for incident {IncidentId}", incident.ID);
        }
    }

}
