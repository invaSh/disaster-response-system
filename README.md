
# Disaster Response System Documentation

## Incident Service

The **Incident Service** manages all incident-related operations in the system and is implemented using the **CQRS (Command Query Responsibility Segregation) pattern** with **MediatR**.
---

## How CQRS Works in This Service

CQRS separates **read operations (queries)** from **write operations (commands)**:

- **Commands**: operations that modify state (e.g., `Create`, `Update`, `Delete`).
- **Queries**: operations that read state (e.g., `GetAll`, `GetOne`).

Each command or query has:
1. A **Command/Query class**: holds the input data.
2. A **Handler class**: contains the business logic for the operation.
3. Optionally, a **Validator** (for commands): ensures input data is valid before processing.
4. Calls the **Service layer**: interacts with the database.
5. Exceptions are wrapped in a structured format by **StatusException** and handled globally by the middleware.

---

## Example Flow: Creating an Incident

1. **Controller Layer**

   The controller receives the HTTP request:

```csharp
[HttpPost]
public async Task<IncidentDTO> Create([FromBody] Create.Command command)
{
    return await _mediator.Send(command);
}
```

The controller does not contain any business logic.

It simply sends the command to MediatR.

### Command Class

```csharp
public class Create
{
    public class Command : IRequest<IncidentDTO>
    {
        public string Title { get; set; }
        public string Description { get; set; }
        // ... other fields
    }
}
```

This class represents the data required to create an incident.

It implements `IRequest<IncidentDTO>`, which tells MediatR what type of response to expect.

### Handler Class

```csharp
public class Handler : IRequestHandler<Create.Command, IncidentDTO>
{
    private readonly IncidentSvc _incidentService;
    private readonly IMapper _mapper;
    private readonly IValidator<Create.Command> _validator;

    public Handler(IncidentSvc incidentService, IMapper mapper, IValidator<Create.Command> validator)
    {
        _incidentService = incidentService;
        _mapper = mapper;
        _validator = validator;
    }

    public async Task<IncidentDTO> Handle(Create.Command request, CancellationToken cancellationToken)
    {
        // 1. Validate the command
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.First().ErrorMessage);

            throw new StatusException(HttpStatusCode.BadRequest, "ValidationError", "Validation failed", errors);
        }

        // 2. Call the service layer
        var incident = await _incidentService.CreateIncident(request);

        // 3. Map the domain entity to a DTO
        return _mapper.Map<IncidentDTO>(incident);
    }
}
```

Key points:

- The Handler is responsible for orchestrating the operation.
- Validation ensures the command is correct before touching the database.
- The handler calls the service to perform the actual database operation.
- Returns a DTO for the controller.

### Service Layer

```csharp
public async Task<Incident> CreateIncident(Create.Command createIncident)
{
    try
    {
        var incident = _mapper.Map<Incident>(createIncident);
        incident.IncidentId = HelperService.GenerateIncidentCode();

        _context.Incidents.Add(incident);
        await _context.SaveChangesAsync();

        return incident;
    }
    catch (Exception ex)
    {
        throw HelperService.MapToStatusException(ex);
    }
}
```

Responsibilities:

- Maps the command to the domain entity.
- Performs the database operation (Add + SaveChangesAsync).
- Wraps any database or server exception using `HelperService.MapToStatusException`.

### Exception Handling

All exceptions are thrown as `StatusException`.

Middleware catches them globally:

```csharp
public async Task InvokeAsync(HttpContext context)
{
    try
    {
        await _next(context);
    }
    catch (StatusException ex)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)ex.StatusCode;
        await context.Response.WriteAsJsonAsync(new
        {
            StatusCode = ex.StatusCode,
            ErrorType = ex.ErrorType,
            Message = ex.Message,
            Errors = ex.Errors,
            Error = ex.Error
        });
    }
}
```

Outcome:

- Validation errors → 400 with structured errors array.
- Database errors → 409 Conflict or appropriate status.
- Unexpected errors → 500 Internal Server Error.

## Summary of Responsibilities

| Layer | Responsibility |
|---|---|
| Controller | Receives HTTP request, sends command/query via MediatR, returns result. |
| Command/Query | Encapsulates the data required for an operation. |
| Handler | Orchestrates the operation: validates input, calls service, maps entity to DTO. |
| Validator | Ensures commands meet business rules before execution. |
| Service | Handles database operations, maps exceptions to StatusException. |
| Middleware | Catches all exceptions, formats a consistent JSON response. |

## Flow Diagram (Simplified)

```
Controller
   │
   ▼
MediatR
   │
   ▼
Handler (validate → call service → map DTO)
   │
   ▼
Service (DB operations → throw StatusException if error)
   │
   ▼
Middleware (catch StatusException → format JSON response)
```

## Key Advantages

- Separation of concerns: controllers only route requests; handlers orchestrate; services persist.
- Centralized error handling: all exceptions return consistent JSON.
- Validation before side effects: ensures commands are correct before touching the database.
- CQRS clarity: queries read, commands write; each has its own path.

This documentation should allow a new developer to understand exactly how CQRS is implemented and how commands, handlers, services, and middleware interact.

---

## Pub/Sub Architecture with SNS/SQS

The system uses **AWS SNS (Simple Notification Service)** and **SQS (Simple Queue Service)** via **LocalStack** for event-driven communication between microservices.

### Architecture Overview

```
IncidentService (Publisher)
    │
    ▼
SNS Topic (incident-created-topic)
    │
    ├──► SQS Queue (dispatch-incident-queue) ──► DispatchService (Consumer)
    │
    └──► SQS Queue (notification-incident-queue) ──► NotificationService (Consumer)
```

### How It Works

1. **Publisher (IncidentService)**: When an incident is created, it publishes an `IncidentCreated` event to an SNS topic.
2. **SNS Topic**: Distributes the event to all subscribed SQS queues.
3. **Consumers (DispatchService & NotificationService)**: Background services poll their respective SQS queues and process messages:
   - **DispatchService**: Creates a dispatch order for the incident
   - **NotificationService**: Creates a notification about the incident

### Directory Structure

```
IncidentService/
  └── Messaging/
      └── Publishers/
          └── IncidentEventPublisher.cs

DispatchService/
  └── Messaging/
      └── Consumers/
          └── IncidentEventConsumer.cs

NotificationService/
  └── Messaging/
      └── Consumers/
          └── IncidentEventConsumer.cs
```

---

## Testing the Pub/Sub Architecture

### Prerequisites

- Docker installed and running
- .NET 8.0 SDK installed
- PostgreSQL running (for database connections)
- All three services configured with correct connection strings

### Step 1: Start LocalStack and Verify Setup

1. **Start LocalStack:**
   ```bash
   docker-compose up -d
   ```

2. **Wait for LocalStack to initialize** (check logs):
   ```bash
   docker logs localstack_main
   ```
   Look for: `Ready.` message indicating LocalStack is ready.

3. **Verify AWS resources were created:**
   ```bash
   # Check SNS topic
   docker exec localstack_main awslocal sns list-topics

   # Check SQS queues
   docker exec localstack_main awslocal sqs list-queues

   # Check subscriptions
   docker exec localstack_main awslocal sns list-subscriptions
   ```

   You should see:
   - SNS topic: `incident-created-topic`
   - SQS queues: `dispatch-incident-queue` and `notification-incident-queue`
   - Two subscriptions linking the queues to the topic

### Step 2: Start Your Services

Start all three services in separate terminals:

**Terminal 1 - IncidentService:**
```bash
cd src/IncidentService
dotnet run
```

**Terminal 2 - DispatchService:**
```bash
cd src/DispatchService
dotnet run
```

**Terminal 3 - NotificationService:**
```bash
cd src/NotificationService
dotnet run
```

**Note:** Make sure each service starts without errors. Check the logs for:
- Database connection success
- AWS services initialized (LocalStack connection)
- Background services started (for DispatchService and NotificationService)

### Step 3: Test the Publisher (Create an Incident)

You can test using Swagger UI, curl, or PowerShell:

**Option A: Using Swagger UI**
1. Navigate to `https://localhost:5001/swagger` (or your IncidentService port)
2. Use the `POST /api/Incident` endpoint
3. Fill in the required fields and execute

**Option B: Using curl (Linux/Mac/Git Bash)**
```bash
curl -X POST "https://localhost:5001/api/Incident" \
  -H "Content-Type: application/json" \
  -d '{
    "title": "Test Fire Incident",
    "description": "Testing pub/sub architecture",
    "type": "Fire",
    "reporterName": "Test User",
    "reporterContact": "test@example.com",
    "latitude": 40.7128,
    "longitude": -74.0060,
    "severity": "High"
  }'
```

**Option C: Using PowerShell (Windows)**
```powershell
$body = @{
    title = "Test Fire Incident"
    description = "Testing pub/sub architecture"
    type = "Fire"
    reporterName = "Test User"
    reporterContact = "test@example.com"
    latitude = 40.7128
    longitude = -74.0060
    severity = "High"
} | ConvertTo-Json

Invoke-RestMethod -Uri "https://localhost:5001/api/Incident" -Method Post -Body $body -ContentType "application/json"
```

**Expected Response:** You should receive a JSON response with the created incident details.

### Step 4: Verify Messages in Queues

Check if messages arrived in the queues:

```bash
# Get queue URLs first (replace with actual URLs from setup script output)
DISPATCH_QUEUE_URL="http://localhost:4566/000000000000/dispatch-incident-queue"
NOTIFICATION_QUEUE_URL="http://localhost:4566/000000000000/notification-incident-queue"

# Check dispatch queue message count
docker exec localstack_main awslocal sqs get-queue-attributes \
  --queue-url "$DISPATCH_QUEUE_URL" \
  --attribute-names ApproximateNumberOfMessages

# Check notification queue message count
docker exec localstack_main awslocal sqs get-queue-attributes \
  --queue-url "$NOTIFICATION_QUEUE_URL" \
  --attribute-names ApproximateNumberOfMessages
```

**Expected Result:** Message count should be `0` or very low (messages are consumed quickly). If messages are accumulating, check consumer logs.

### Step 5: Verify Consumers Processed Messages

Check the logs for each service:

**DispatchService logs should show:**
```
IncidentEventConsumer started. Polling queue: http://localhost:4566/...
Received 1 messages from queue
Processing IncidentCreated event for incident {IncidentId}
Created dispatch order for incident {IncidentId}
```

**NotificationService logs should show:**
```
IncidentEventConsumer started. Polling queue: http://localhost:4566/...
Received 1 messages from queue
Processing IncidentCreated event for incident {IncidentId}
Created notification for incident {IncidentId}
```

### Step 6: Verify Data Was Created

**Check DispatchService:**
```bash
# Using Swagger or curl
curl https://localhost:5002/api/dispatchorders
```

You should see a dispatch order with the incident ID you created.

**Check NotificationService:**
```bash
# Using Swagger or curl
curl https://localhost:5003/api/notification
```

You should see a notification related to the incident you created.

### Step 7: Monitor Queue Messages in Real-Time

Watch queue messages as they come in (Linux/Mac):

```bash
# Watch dispatch queue
watch -n 2 'docker exec localstack_main awslocal sqs get-queue-attributes --queue-url http://localhost:4566/000000000000/dispatch-incident-queue --attribute-names ApproximateNumberOfMessages'

# Watch notification queue
watch -n 2 'docker exec localstack_main awslocal sqs get-queue-attributes --queue-url http://localhost:4566/000000000000/notification-incident-queue --attribute-names ApproximateNumberOfMessages'
```

### Step 8: Test Error Scenarios

1. **Test Message Persistence:**
   - Stop DispatchService
   - Create an incident
   - Check queue: messages should accumulate
   - Restart DispatchService
   - Messages should be processed automatically

2. **Test Consumer Resilience:**
   - Create multiple incidents rapidly
   - Verify all messages are processed
   - Check that no messages are lost

### Quick Verification Checklist

- [ ] LocalStack is running and resources are created
- [ ] All three services start without errors
- [ ] Creating an incident returns success
- [ ] DispatchService logs show message consumption
- [ ] NotificationService logs show message consumption
- [ ] Dispatch order is created in DispatchService
- [ ] Notification is created in NotificationService
- [ ] Queue message count decreases after processing

### Troubleshooting

#### Messages aren't flowing:

1. **Check LocalStack logs:**
   ```bash
   docker logs localstack_main --tail 50
   ```

2. **Verify SNS subscription:**
   ```bash
   docker exec localstack_main awslocal sns list-subscriptions-by-topic \
     --topic-arn arn:aws:sns:us-east-1:000000000000:incident-created-topic
   ```

3. **Check service logs for errors:**
   - Look for AWS connection errors
   - Verify queue names match configuration
   - Check for JSON deserialization errors

4. **Verify appsettings.json configuration:**
   - `ServiceURL: http://localhost:4566`
   - Correct queue names in each service
   - Correct topic name in IncidentService

5. **Test SNS publish manually:**
   ```bash
   docker exec localstack_main awslocal sns publish \
     --topic-arn arn:aws:sns:us-east-1:000000000000:incident-created-topic \
     --message '{"EventType":"IncidentCreated","Data":{"ID":"test-123"}}'
   ```

#### Services can't connect to LocalStack:

1. **Verify LocalStack is accessible:**
   ```bash
   curl http://localhost:4566/health
   ```

2. **Check firewall/network settings:**
   - Ensure port 4566 is not blocked
   - Verify Docker network configuration

3. **Check service configuration:**
   - Verify `AWS:ServiceURL` in appsettings.json
   - Ensure services are using the correct endpoint

#### Messages are stuck in queue:

1. **Check consumer is running:**
   - Verify background service is registered in `Program.cs`
   - Check logs for consumer startup messages

2. **Check for processing errors:**
   - Look for exceptions in consumer logs
   - Verify database connections are working
   - Check for validation errors

3. **Manually peek at queue messages:**
   ```bash
   docker exec localstack_main awslocal sqs receive-message \
     --queue-url http://localhost:4566/000000000000/dispatch-incident-queue
   ```

### Configuration Reference

**IncidentService/appsettings.json:**
```json
{
  "AWS": {
    "ServiceURL": "http://localhost:4566",
    "Region": "us-east-1",
    "SNS": {
      "IncidentCreatedTopic": "incident-created-topic"
    }
  }
}
```

**DispatchService/appsettings.json:**
```json
{
  "AWS": {
    "ServiceURL": "http://localhost:4566",
    "Region": "us-east-1",
    "SQS": {
      "QueueName": "dispatch-incident-queue"
    }
  }
}
```

**NotificationService/appsettings.json:**
```json
{
  "AWS": {
    "ServiceURL": "http://localhost:4566",
    "Region": "us-east-1",
    "SQS": {
      "QueueName": "notification-incident-queue"
    }
  }
}
```

---

## Summary

This pub/sub architecture enables:
- **Decoupled communication** between microservices
- **Reliable message delivery** via SQS queues
- **Scalable event distribution** via SNS topics
- **Resilient processing** with background consumers
- **Easy local development** with LocalStack

The separation of publishers and consumers in the `Messaging` directory keeps infrastructure concerns separate from business logic.