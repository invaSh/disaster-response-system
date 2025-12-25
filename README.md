
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
