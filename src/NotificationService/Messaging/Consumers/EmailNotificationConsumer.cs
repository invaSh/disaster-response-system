using Amazon.SQS;
using Amazon.SQS.Model;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using NotificationService.Services;

namespace NotificationService.Messaging.Consumers;

public class EmailNotificationConsumer : BackgroundService
{
    private readonly IAmazonSQS _sqs;
    private readonly IConfiguration _config;
    private readonly ILogger<EmailNotificationConsumer> _logger;
    private readonly IEmailSender _emailSender;

    private string? _queueUrl;

    public EmailNotificationConsumer(
        IAmazonSQS sqs,
        IConfiguration config,
        ILogger<EmailNotificationConsumer> logger,
        IEmailSender emailSender)
    {
        _sqs = sqs;
        _config = config;
        _logger = logger;
        _emailSender = emailSender;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await InitializeQueueAsync(stoppingToken);

        _logger.LogInformation("EmailNotificationConsumer started. Polling queue: {QueueUrl}", _queueUrl);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var response = await _sqs.ReceiveMessageAsync(new ReceiveMessageRequest
                {
                    QueueUrl = _queueUrl,
                    MaxNumberOfMessages = 10,
                    WaitTimeSeconds = 20,
                    MessageAttributeNames = new List<string> { "All" }
                }, stoppingToken);

                if (response.Messages is null || !response.Messages.Any())
                    continue;

                foreach (var msg in response.Messages)
                    await ProcessMessageAsync(msg, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error receiving email notification messages from SQS");
                await Task.Delay(3000, stoppingToken);
            }
        }
    }

    private async Task InitializeQueueAsync(CancellationToken ct)
    {
        var queueName = _config["AWS:SQS:EmailQueueName"] ?? "notification-email-queue";

        try
        {
            var queueUrlResponse = await _sqs.GetQueueUrlAsync(queueName, ct);
            _queueUrl = queueUrlResponse.QueueUrl;
            _logger.LogInformation("Initialized Email queue URL: {QueueUrl}", _queueUrl);
        }
        catch (QueueDoesNotExistException)
        {
            _logger.LogWarning("Queue {QueueName} does not exist. Will retry...", queueName);
            await Task.Delay(5000, ct);
            await InitializeQueueAsync(ct);
        }
    }

    private async Task ProcessMessageAsync(Message message, CancellationToken ct)
    {
        try
        {
            // SNS -> SQS wrap
            var sns = JsonSerializer.Deserialize<SnsEnvelope>(message.Body, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (sns is null || string.IsNullOrWhiteSpace(sns.Message))
            {
                _logger.LogWarning("Invalid SNS envelope. Deleting message.");
                await DeleteAsync(message.ReceiptHandle);
                return;
            }

            var evt = JsonSerializer.Deserialize<EmailNotificationEvent>(sns.Message, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (evt is null || evt.EventType != "NotificationEmailRequested" || evt.Data is null)
            {
                _logger.LogWarning("Invalid event. Deleting message.");
                await DeleteAsync(message.ReceiptHandle);
                return;
            }

            var recipients = evt.Data.Recipients ?? new List<string>();
            _logger.LogInformation("Processing email event for {Count} recipients", recipients.Count);

            foreach (var to in recipients)
            {
                if (string.IsNullOrWhiteSpace(to)) continue;
                await _emailSender.SendAsync(to, evt.Data.Subject ?? "", evt.Data.Body ?? "", ct);
            }

            await DeleteAsync(message.ReceiptHandle);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing email notification message (will retry).");
            // mos e fshi se SQS e retry
        }
    }

    private Task DeleteAsync(string receiptHandle)
        => _sqs.DeleteMessageAsync(_queueUrl, receiptHandle);

    private class SnsEnvelope
    {
        public string Type { get; set; } = "";
        public string Message { get; set; } = "";
        public string TopicArn { get; set; } = "";
    }

    private class EmailNotificationEvent
    {
        public string EventType { get; set; } = "";
        public DateTime Timestamp { get; set; }
        public EmailData? Data { get; set; }
    }

    private class EmailData
    {
        public string? Subject { get; set; }
        public string? Body { get; set; }
        public List<string>? Recipients { get; set; }
    }
}
