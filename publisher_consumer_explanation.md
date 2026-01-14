# Publisher and Consumer Explanation

This document contains all explanations and code snippets from the conversation about the IncidentEventPublisher and IncidentEventConsumer implementation.

## Table of Contents
1. [Publisher: Why use `_` and Task.Run](#publisher-why-use-_-and-taskrun)
2. [Publisher: Topic Name, Topics Response, and Topic ARN](#publisher-topic-name-topics-response-and-topic-arn)
3. [What is ARN](#what-is-arn)
4. [Publisher: JSON Serialization and camelCase](#publisher-json-serialization-and-camelcase)
5. [How does it know which queue to publish to](#how-does-it-know-which-queue-to-publish-to)
6. [Why is the consumer a class and not an interface](#why-is-the-consumer-a-class-and-not-an-interface)
7. [Why as a background service](#why-as-a-background-service)
8. [How does it know which queue to initialize](#how-does-it-know-which-queue-to-initialize)
9. [InitializeQueueAsync Line-by-Line Explanation](#initializequeueasync-line-by-line-explanation)
10. [Why the while condition](#why-the-while-condition)
11. [ReceiveRequest Object Explanation](#receiverequest-object-explanation)
12. [What kind of class is ReceiveMessageRequest](#what-kind-of-class-is-receivemessagerequest)
13. [What is considered a message in MaxNumberOfMessages](#what-is-considered-a-message-in-maxnumberofmessages)
14. [Message Processing Loop Explanation](#message-processing-loop-explanation)
15. [Why Deserialize and JsonSerializerOptions](#why-deserialize-and-jsonserializeroptions)
16. [Why delete message](#why-delete-message)
17. [What is receipt handle](#what-is-receipt-handle)
18. [What does receipt handle look like](#what-does-receipt-handle-look-like)
19. [Why add custom class for SNS](#why-add-custom-class-for-sns)
20. [IncidentCreatedEvent and IncidentData Explanation](#incidentcreatedevent-and-incidentdata-explanation)
21. [What are scopes](#what-are-scopes)
22. [Dispatch Order Creation Explanation](#dispatch-order-creation-explanation)

---

## Publisher: Why use `_` and Task.Run

**Location:** `src/IncidentService/Application/Incident/Create.cs:118`

```csharp
_ = Task.Run(async () => await _eventPublisher.PublishIncidentCreatedAsync(incidentDto), cancellationToken);
```

### Why use `_` as variable name?

`_` is a **discard** in C#. It's a special pattern that tells the compiler "I'm intentionally ignoring this return value." In this case, `Task.Run` returns a `Task`, but since this is a "fire and forget" operation (as indicated by the comment), the code doesn't need to await or track the task. Using `_` makes this intent explicit to other developers.

### Why use Task.Run and where is it coming from?

`Task.Run` is a static method from `System.Threading.Tasks` namespace (likely available via implicit usings in .NET 6+). It queues work to run on the thread pool.

**However, there are issues with this pattern:**

1. The `cancellationToken` passed to `Task.Run` doesn't actually cancel the async operation inside the lambda.
2. Unobserved exceptions will be swallowed silently.
3. In ASP.NET Core, this can cause issues if the request completes before the task finishes.

**Better alternatives for fire-and-forget in ASP.NET Core:**

```csharp
// Option 1: Use a background service or hosted service
// Option 2: Use IHostApplicationLifetime to track background work
// Option 3: Use a proper message queue/background job system
// Option 4: If you must fire-and-forget, at least handle exceptions:

_ = Task.Run(async () => 
{
    try
    {
        await _eventPublisher.PublishIncidentCreatedAsync(incidentDto);
    }
    catch (Exception ex)
    {
        // Log the exception - don't swallow it silently
        // _logger.LogError(ex, "Failed to publish incident created event");
    }
}, cancellationToken);
```

The current pattern can hide failures and isn't reliable for critical operations like event publishing.

---

## Publisher: Topic Name, Topics Response, and Topic ARN

**Location:** `src/IncidentService/Messaging/Publishers/IncidentEventPublisher.cs:33-38`

```csharp
var topicName = _configuration["AWS:SNS:IncidentCreatedTopic"] ?? "incident-created-topic";

// Get topic ARN by listing topics and finding the one that matches
var topicsResponse = await _snsClient.ListTopicsAsync();
var topicArn = topicsResponse.Topics
    .FirstOrDefault(t => t.TopicArn.Contains(topicName))?.TopicArn;
```

### What do these 3 vars do?

These three variables work together to find the AWS SNS topic ARN needed for publishing the event:

1. **`topicName`** (line 33)
   - Reads the topic name from configuration (`AWS:SNS:IncidentCreatedTopic`)
   - Falls back to `"incident-created-topic"` if not configured
   - This is just the logical name, not the full ARN

2. **`topicsResponse`** (line 36)
   - Calls AWS SNS to list all topics in the account/region
   - Returns a response containing a collection of topics

3. **`topicArn`** (line 37-38)
   - Searches through the list to find a topic whose ARN contains the `topicName`
   - Returns the full ARN if found, or `null` if not found
   - The ARN is required to actually publish to the topic

**Why this approach?**
- SNS requires the full ARN to publish, not just the name
- The ARN format is like: `arn:aws:sns:region:account-id:topic-name`
- This code finds the ARN by matching the topic name within the ARN string

**Note:** If `topicArn` is `null` (lines 40-49), the code creates the topic and uses its ARN.

---

## What is ARN

**ARN** stands for **Amazon Resource Name**. It's a unique identifier for AWS resources.

### Format

An ARN follows this pattern:
```
arn:partition:service:region:account-id:resource-type/resource-id
```

### Example for SNS Topic

```
arn:aws:sns:us-east-1:123456789012:incident-created-topic
```
- `arn:aws` - AWS partition
- `sns` - Simple Notification Service
- `us-east-1` - Region
- `123456789012` - AWS account ID
- `incident-created-topic` - Topic name

### Why ARNs Matter

- AWS APIs often require ARNs to identify resources
- They uniquely identify resources across accounts and regions
- They enable cross-account and cross-region references

### In Your Code Context

The code needs the topic ARN to publish to SNS. It:
1. Gets the topic name from config (e.g., `"incident-created-topic"`)
2. Lists all SNS topics to find the one matching that name
3. Extracts the full ARN (e.g., `arn:aws:sns:us-east-1:123456789012:incident-created-topic`)
4. Uses that ARN in the `PublishRequest` to send the message

You can't publish with just the topic name; you need the full ARN.

---

## Publisher: JSON Serialization and camelCase

**Location:** `src/IncidentService/Messaging/Publishers/IncidentEventPublisher.cs:58-61`

```csharp
var messageBody = JsonSerializer.Serialize(eventMessage, new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
});
```

### Why JSON Serialization is Necessary

The SNS `PublishRequest.Message` property requires a **string**. The `eventMessage` object (lines 51-56) needs to be converted to JSON before it can be sent.

### Why camelCase?

The consumers use `PropertyNameCaseInsensitive = true` when deserializing (lines 94-97 and 106-109 in both consumers), so they can handle both PascalCase and camelCase. However, using camelCase is still a best practice:

1. **Web/JavaScript convention**: camelCase is the standard for JSON in web APIs
2. **Consistency**: Keeps a predictable format across services
3. **Interoperability**: Other services or frontends may expect camelCase
4. **Explicit contract**: Makes the expected format clear

**Without camelCase policy:**
```json
{
  "EventType": "IncidentCreated",
  "Timestamp": "2024-01-01T00:00:00Z",
  "Data": { ... }
}
```

**With camelCase policy:**
```json
{
  "eventType": "IncidentCreated",
  "timestamp": "2024-01-01T00:00:00Z",
  "data": { ... }
}
```

Since the consumers use case-insensitive matching, it works either way, but camelCase aligns with common JSON conventions and improves compatibility.

---

## How does it know which queue to publish to

The publisher **doesn't publish directly to queues**. It uses a **pub/sub pattern** with SNS.

### How It Works

1. **Publisher publishes to an SNS topic** (not a queue)
   - The `IncidentEventPublisher` publishes to the SNS topic `incident-created-topic` (line 33 in the publisher)

2. **SQS queues subscribe to the SNS topic**
   - This is configured in infrastructure setup, not in the application code
   - Looking at `init-aws/setup.sh` (lines 24-33), the queues are subscribed to the topic:

```bash
# Subscribe queues to SNS topic
awslocal sns subscribe \
  --topic-arn "$INCIDENT_TOPIC_ARN" \
  --protocol sqs \
  --notification-endpoint "$DISPATCH_QUEUE_ARN"

awslocal sns subscribe \
  --topic-arn "$INCIDENT_TOPIC_ARN" \
  --protocol sqs \
  --notification-endpoint "$NOTIFICATION_QUEUE_ARN"
```

3. **SNS automatically distributes messages**
   - When a message is published to the topic, SNS delivers it to all subscribed queues
   - The publisher doesn't need to know which queues exist

### Architecture Flow

```
IncidentService (Publisher)
    │
    ▼
SNS Topic (incident-created-topic)  ← Publisher only knows about this
    │
    ├──► SQS Queue (dispatch-incident-queue) ──► DispatchService
    │
    └──► SQS Queue (notification-incident-queue) ──► NotificationService
```

### Benefits

- **Decoupling**: Publisher doesn't need to know about consumers
- **Scalability**: Add new consumers by subscribing new queues to the topic
- **Reliability**: SNS handles delivery to all subscribers

The publisher only needs the topic name/ARN; SNS handles routing to subscribed queues.

---

## Why is the consumer a class and not an interface

The difference comes from how they're used:

### Publisher (with interface)

**Why it has an interface:**
- Injected into other classes (e.g., `Create.cs` handler line 80)
- Registered as a scoped service: `AddScoped<IIncidentEventPublisher, IncidentEventPublisher>` (line 32 in `IncidentService/Program.cs`)
- Called on-demand when incidents are created
- Interface enables:
  - Dependency injection
  - Testability (mock the interface)
  - Loose coupling

### Consumer (no interface)

**Why it doesn't have an interface:**
- Inherits from `BackgroundService` (line 11) - it's a long-running background service
- Registered as a hosted service: `AddHostedService<IncidentEventConsumer>()` (line 41 in `DispatchService/Program.cs`)
- Not injected into other classes; runs independently
- Runs continuously, polling the SQS queue in a loop (lines 37-66)
- No interface needed because:
  - Nothing depends on it
  - It's registered directly as a hosted service
  - The framework manages its lifecycle

### Summary

| Aspect | Publisher | Consumer |
|--------|-----------|----------|
| **Usage** | On-demand, injected | Long-running background service |
| **Registration** | `AddScoped<IInterface, Implementation>` | `AddHostedService<Class>()` |
| **Interface needed?** | Yes (for DI and testing) | No (nothing depends on it) |
| **Lifecycle** | Created per request/scope | Runs for the app lifetime |

You could add an interface to the consumer for testing purposes, but it's not necessary since nothing depends on it.

---

## Why as a background service

The consumer needs to **continuously poll the SQS queue** for messages. This requires a long-running process that:

1. Runs independently of HTTP requests
2. Starts when the application starts
3. Runs continuously in the background
4. Stops gracefully when the application shuts down

### How BackgroundService Works

Looking at the code:

```csharp
protected override async Task ExecuteAsync(CancellationToken stoppingToken)
{
    // Initialize queue...
    
    while (!stoppingToken.IsCancellationRequested)  // ← Runs continuously
    {
        // Poll SQS queue for messages
        var response = await _sqsClient.ReceiveMessageAsync(...);
        
        // Process messages...
    }
}
```

The `ExecuteAsync` method:
- Runs automatically when the app starts (managed by ASP.NET Core)
- Loops continuously until `stoppingToken` is cancelled
- Handles graceful shutdown when the app stops

### Alternatives (and why they don't fit)

1. **Controller endpoint**: Only runs when called via HTTP; can't poll continuously
2. **On-demand service**: Would require manual triggering; messages need to be processed automatically
3. **Timer-based**: Could work, but `BackgroundService` provides better lifecycle management and cancellation support

### Benefits of BackgroundService

- **Automatic lifecycle**: Starts/stops with the application
- **Graceful shutdown**: `stoppingToken` signals when to stop
- **Resource management**: Framework manages the task lifecycle
- **Integration**: Works seamlessly with ASP.NET Core dependency injection

This is the standard pattern for queue consumers in ASP.NET Core.

---

## How does it know which queue to initialize

**Location:** `src/DispatchService/Messaging/Consumers/IncidentEventConsumer.cs:33`

Reads from configuration:

```csharp
var queueName = _configuration["AWS:SQS:QueueName"] ?? "dispatch-incident-queue";
```

Gets the queue name from `appsettings.json` under `AWS:SQS:QueueName`, or defaults to `"dispatch-incident-queue"` if not set.

---

## InitializeQueueAsync Line-by-Line Explanation

**Location:** `src/DispatchService/Messaging/Consumers/IncidentEventConsumer.cs:69-86`

```csharp
private async Task InitializeQueueAsync(CancellationToken cancellationToken)
```
Defines an async method to initialize the SQS queue URL. `cancellationToken` allows cancellation.

```csharp
var queueName = _configuration["AWS:SQS:QueueName"] ?? "dispatch-incident-queue";
```
Reads the queue name from configuration (`AWS:SQS:QueueName`). If missing, defaults to `"dispatch-incident-queue"`.

```csharp
try
```
Starts error handling for queue initialization.

```csharp
var queueUrlResponse = await _sqsClient.GetQueueUrlAsync(queueName, cancellationToken);
```
Calls AWS SQS to get the queue URL by name. Returns a response with the URL.

```csharp
_queueUrl = queueUrlResponse.QueueUrl;
```
Stores the queue URL in the instance field for later use.

```csharp
_logger.LogInformation("Initialized queue URL: {QueueUrl}", _queueUrl);
```
Logs that the queue URL was initialized.

```csharp
catch (QueueDoesNotExistException)
```
Catches the exception when the queue doesn't exist.

```csharp
_logger.LogWarning("Queue {QueueName} does not exist. Will retry...", queueName);
```
Logs a warning that the queue is missing and will retry.

```csharp
// Queue will be created by setup script, so we'll retry
```
Comment noting the queue is created by a setup script, so retrying is expected.

```csharp
await Task.Delay(5000, cancellationToken);
```
Waits 5 seconds before retrying.

```csharp
await InitializeQueueAsync(cancellationToken);
```
Recursively calls itself to retry initialization.

**Summary:** Reads the queue name from config, gets the queue URL from AWS SQS, and stores it. If the queue doesn't exist, waits 5 seconds and retries recursively until it's available.

---

## Why the while condition

**Location:** `src/DispatchService/Messaging/Consumers/IncidentEventConsumer.cs:37`

```csharp
while (!stoppingToken.IsCancellationRequested)
```

The loop runs until the application shuts down.

**`stoppingToken`** is a `CancellationToken` provided by `BackgroundService` that signals when the app is stopping.

**`!stoppingToken.IsCancellationRequested`** means "continue while cancellation has not been requested."

**Why this pattern:**
- Keeps the consumer polling continuously while the app runs
- Allows graceful shutdown: when the app stops, the token is signaled and the loop exits
- Prevents the loop from running after shutdown

Without this check, the loop would run indefinitely even after the app stops, causing resource leaks or hanging shutdown.

---

## ReceiveRequest Object Explanation

**Location:** `src/DispatchService/Messaging/Consumers/IncidentEventConsumer.cs:39-47`

```csharp
try
{
    var receiveRequest = new ReceiveMessageRequest
    {
        QueueUrl = _queueUrl,
        MaxNumberOfMessages = 10,
        WaitTimeSeconds = 20, // Long polling
        MessageAttributeNames = new List<string> { "All" }
    };
```

The `ReceiveMessageRequest` configures how messages are retrieved from the SQS queue:

```csharp
var receiveRequest = new ReceiveMessageRequest
```
Creates a request object to receive messages from SQS.

```csharp
QueueUrl = _queueUrl,
```
Specifies which queue to poll (the URL initialized earlier).

```csharp
MaxNumberOfMessages = 10,
```
Maximum number of messages to retrieve per call (1-10). Reduces API calls when multiple messages are available.

```csharp
WaitTimeSeconds = 20, // Long polling
```
Enables long polling: waits up to 20 seconds for messages instead of returning immediately. Reduces empty responses and API calls.

```csharp
MessageAttributeNames = new List<string> { "All" }
```
Requests all message attributes (like `EventType` from the publisher). Needed to access metadata sent with the message.

**Summary:** Configures SQS to retrieve up to 10 messages at a time from the specified queue, using long polling (wait up to 20 seconds), and include all message attributes.

---

## What kind of class is ReceiveMessageRequest

`ReceiveMessageRequest` is a **request class** from the AWS SDK for .NET.

- **Namespace:** `Amazon.SQS.Model` (imported on line 2)
- **Type:** Parameter/configuration object (DTO-like)
- **Purpose:** Encapsulates parameters for the `ReceiveMessageAsync` call

It's a plain data class with properties that configure how SQS retrieves messages. Similar to other AWS SDK request classes (like `PublishRequest`, `CreateTopicRequest`).

---

## What is considered a message in MaxNumberOfMessages

A "message" in SQS is a **single unit of data** in the queue.

In this context, each message is:
- An SNS-wrapped message containing an `IncidentCreated` event
- Published by `IncidentEventPublisher` when an incident is created
- Includes the message body (JSON with event data) and message attributes

**Example:**
- If 15 incidents are created, the queue has 15 messages
- With `MaxNumberOfMessages = 10`, one `ReceiveMessageAsync` call returns up to 10 messages
- The next call returns the remaining 5

Each message is processed individually (lines 55-58 loop through `response.Messages`), so `MaxNumberOfMessages = 10` means up to 10 incident events can be retrieved in one API call.

---

## Message Processing Loop Explanation

**Location:** `src/DispatchService/Messaging/Consumers/IncidentEventConsumer.cs:50-59`

```csharp
if (response.Messages != null && response.Messages.Any())
```
Checks if the response contains messages.

```csharp
_logger.LogInformation("Received {Count} messages from queue", response.Messages.Count);
```
Logs how many messages were received.

```csharp
foreach (var message in response.Messages)
```
Iterates through each message.

```csharp
await ProcessMessageAsync(message, stoppingToken);
```
Processes each message individually (deserializes, validates, creates dispatch order, deletes from queue).

**Summary:** If messages exist, log the count, then process each one sequentially.

---

## Why Deserialize and JsonSerializerOptions

**Location:** `src/DispatchService/Messaging/Consumers/IncidentEventConsumer.cs:94-97`

```csharp
var snsMessage = JsonSerializer.Deserialize<SNSMessage>(message.Body, new JsonSerializerOptions
{
    PropertyNameCaseInsensitive = true
});
```

### Why deserialize?

`message.Body` is a **JSON string**. SNS wraps the original message when delivering to SQS, so it needs to be parsed into a C# object.

### Why JsonSerializer?

`System.Text.Json.JsonSerializer` converts JSON strings into C# objects. `message.Body` looks like:
```json
{
  "Type": "Notification",
  "Message": "{\"eventType\":\"IncidentCreated\",...}",
  "TopicArn": "arn:aws:sns:..."
}
```

### What is JsonSerializerOptions?

Configuration for how JSON is parsed (naming conventions, null handling, etc.).

### What is PropertyNameCaseInsensitive?

Makes property matching case-insensitive. JSON like `{"type": "..."}` will match a C# property `Type`. Without it, only exact case matches (e.g., `"Type"` matches `Type`).

### Why needed here?

SNS may send property names in different cases, so this ensures the deserializer finds properties like `Type`, `Message`, `TopicArn` regardless of casing.

---

## Why delete message

**Location:** `src/DispatchService/Messaging/Consumers/IncidentEventConsumer.cs:98-105`

```csharp
if (snsMessage == null || string.IsNullOrEmpty(snsMessage.Message))
{
    _logger.LogWarning("Invalid SNS message format. Deleting message.");
    await DeleteMessageAsync(message.ReceiptHandle);
    return;
}
```

**Why delete:**
The message is invalid (null or empty). If not deleted, SQS will keep redelivering it. Deleting removes the bad message so the consumer can process valid ones.

**SQS behavior:** Messages must be explicitly deleted after processing. If not deleted, they become visible again after the visibility timeout and are redelivered.

---

## What is receipt handle

**Location:** `src/DispatchService/Messaging/Consumers/IncidentEventConsumer.cs:145-155`

```csharp
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
```

**Receipt handle** is a unique identifier SQS assigns to each received message.

**Purpose:**
- Identifies the specific message to delete
- Required to delete a message from the queue
- Included in the `Message` object returned by `ReceiveMessageAsync` (line 49)

**Why needed:**
SQS requires the receipt handle to delete a message. It prevents deleting the wrong message if the same message body appears multiple times.

**Flow:**
1. Receive message → get `message.ReceiptHandle`
2. Process message
3. Delete using `message.ReceiptHandle` (line 135, 102, 114)

Without the receipt handle, you can't delete the message from the queue.

---

## What does receipt handle look like

A receipt handle is a long, opaque string that SQS generates. Example:

```
AQEBzWwaftRI0KuVm4tP+/7q1rGgNqicHq...
```

**Characteristics:**
- Long string (often 100+ characters)
- Opaque (don't parse it)
- Unique per message receive
- Expires after the visibility timeout
- Format varies by AWS region/service

**Example (LocalStack/SQS):**
```
AQEBzWwaftRI0KuVm4tP+/7q1rGgNqicHq8q+8t1j2k3l4m5n6o7p8q9r0s1t2u3v4w5x6y7z8a9b0c1d2e3f4g5h6i7j8k9l0m1n2o3p4q5r6s7t8u9v0w1x2y3z4a5b6c7d8e9f0g1h2i3j4k5l6m7n8o9p0q1r2s3t4u5v6w7x8y9z0
```

**Important:** Don't store or reuse it. It's only valid for the specific message receive and expires. Use it immediately to delete the message.

---

## Why add custom class for SNS

**Location:** `src/DispatchService/Messaging/Consumers/IncidentEventConsumer.cs:94-97`

```csharp
var snsMessage = JsonSerializer.Deserialize<SNSMessage>(message.Body, new JsonSerializerOptions
{
    PropertyNameCaseInsensitive = true
});
```

**Why a custom class:**
The AWS SDK doesn't provide an `SNSMessage` class. When SNS delivers to SQS, it wraps the original message in a JSON structure with `Type`, `Message`, and `TopicArn`. This class matches that structure.

**Why inside the file:**
- Private helper used only for deserialization in this consumer
- Simple DTO that matches the SNS wrapper format
- Not shared with other code
- Keeps the deserialization logic self-contained

**What it represents:**
When SNS sends to SQS, the message body looks like:
```json
{
  "Type": "Notification",
  "Message": "{\"eventType\":\"IncidentCreated\",...}",  // ← Your actual event (as string)
  "TopicArn": "arn:aws:sns:..."
}
```

The `SNSMessage` class lets `JsonSerializer.Deserialize` convert this JSON into a C# object so you can extract `snsMessage.Message` (the actual event data) and deserialize it again.

---

## IncidentCreatedEvent and IncidentData Explanation

**Location:** `src/DispatchService/Messaging/Consumers/IncidentEventConsumer.cs:106-109, 163-180`

```csharp
var incidentEvent = JsonSerializer.Deserialize<IncidentCreatedEvent>(snsMessage.Message, new JsonSerializerOptions
{
    PropertyNameCaseInsensitive = true
});
```

```csharp
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
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}
```

**`IncidentCreatedEvent`** - represents the event wrapper that was published by `IncidentEventPublisher`:
- `EventType`: The type of event (e.g., "IncidentCreated")
- `Timestamp`: When the event occurred
- `Data`: The actual incident data (wrapped in `IncidentData`)

**`IncidentData`** - represents the actual incident details:
- `ID`, `IncidentId`, `Title`, `Description`, `Type`, `Severity`, `Latitude`, `Longitude`

**What they do:**
Deserialize the JSON from `snsMessage.Message` (the actual event published by the publisher). The publisher creates:
```json
{
  "eventType": "IncidentCreated",
  "timestamp": "...",
  "data": { /* incident details */ }
}
```

These classes map that structure so you can access `incidentEvent.Data.ID` (line 127) and `incidentEvent.Data.Title` (line 128) to create the dispatch order.

**Why separate classes:**
- `IncidentCreatedEvent` = event envelope (metadata)
- `IncidentData` = the incident payload

This matches the structure published by `IncidentEventPublisher` (lines 51-56 in that file).

---

## What are scopes

**Location:** `src/DispatchService/Messaging/Consumers/IncidentEventConsumer.cs:121-122`

```csharp
using var scope = _serviceProvider.CreateScope();
var dispatchService = scope.ServiceProvider.GetRequiredService<DispatchSvc>();
```

**The problem:**
- `IncidentEventConsumer` is a singleton (lives for the app lifetime)
- `DispatchSvc` is scoped (line 38 in Program.cs)
- Scoped services are created per HTTP request and disposed after
- Background services don't have HTTP requests, so there's no automatic scope

**The solution:**
```csharp
using var scope = _serviceProvider.CreateScope();
```
Creates a new dependency injection scope manually.

```csharp
var dispatchService = scope.ServiceProvider.GetRequiredService<DispatchSvc>();
```
Gets `DispatchSvc` from that scope (creates a new instance).

**Why `using`:**
The `using` statement disposes the scope when done, which disposes all scoped services (like DbContext) and releases resources.

**Without the scope:**
You can't get scoped services from a singleton. Creating a scope gives you a "request-like" lifetime for background processing.

**Summary:** Creates a temporary scope to resolve scoped services (`DispatchSvc`, DbContext, etc.) in a background service, then cleans up when done.

---

## Dispatch Order Creation Explanation

**Location:** `src/DispatchService/Messaging/Consumers/IncidentEventConsumer.cs:125-135`

```csharp
var createOrderDto = new CreateDispatchOrderRequestDTO
```
Creates a DTO object to request a dispatch order.

```csharp
IncidentId = Guid.Parse(incidentEvent.Data!.ID),
```
Parses the incident ID from the event (string to Guid).

```csharp
Notes = $"Auto-created from incident: {incidentEvent.Data.Title}"
```
Sets a note indicating the order was auto-created from the incident.

```csharp
await dispatchService.CreateDispatchOrder(createOrderDto, cancellationToken);
```
Calls the dispatch service to create the order in the database.

```csharp
_logger.LogInformation("Created dispatch order for incident {IncidentId}", incidentEvent.Data.ID);
```
Logs that the dispatch order was created.

```csharp
// Delete the message after successful processing
await DeleteMessageAsync(message.ReceiptHandle);
```
Deletes the message from the queue after successful processing to prevent reprocessing.

**Summary:** Creates a dispatch order from the incident event data, saves it, logs success, then deletes the message from the queue.

---

## End of Document

This document contains all explanations and code snippets from the conversation about the publisher and consumer implementation.
