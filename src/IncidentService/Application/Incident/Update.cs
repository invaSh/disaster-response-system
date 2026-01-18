using AutoMapper;
using FluentValidation;
using IncidentService.DTOs;
using IncidentService.Enums;
using IncidentService.Middlewares;
using IncidentService.Services;
using IncidentService.Messaging.Publishers;
using MediatR;
using System.Net;
using Microsoft.AspNetCore.Http;

namespace IncidentService.Application.Incident
{
    public class Update
    {
        public class Command : IRequest<Unit>
        {
            public Guid ID { get; set; }
            public string? Title { get; set; }
            public string? Description { get; set; }
            public string? Type { get; set; }

            public string? ReporterName { get; set; }
            public string? ReporterContact { get; set; }

            public double? Latitude { get; set; }
            public double? Longitude { get; set; }

            public DateTime? ResolvedAt { get; set; }
            public string? ResolutionNotes { get; set; }

            public string? Status { get; set; }

            public string? Severity { get; set; }

            public List<Guid>? AssignedUnits { get; set; }

            public List<UpdateDTO>? Updates { get; set; }

            public List<IFormFile>? MediaFiles { get; set; }

            public Dictionary<string, string>? Metadata { get; set; }
        }

        public class CommandValidator : AbstractValidator<Command>
        {
            public CommandValidator()
            {
                RuleFor(x => x.Title)
                    .NotEmpty().WithMessage("Title is required.");

                RuleFor(x => x.Description)
                    .NotEmpty().WithMessage("Description is required.");

                RuleFor(x => x.Latitude).NotEmpty().WithMessage("Latitude is required");

                RuleFor(x => x.Longitude).NotEmpty().WithMessage("Longitude is required.");

                RuleFor(x => x.Type)
                    .Must(BeValidIncidentType).WithMessage("Invalid incident category")
                    .When(x => !string.IsNullOrWhiteSpace(x.Type));

                RuleFor(x => x.Severity)
                    .Must(BeValidSeverity).WithMessage("Invalid severity category.")
                    .When(x => !string.IsNullOrWhiteSpace(x.Severity));

                RuleFor(x => x.ReporterName)
                     .Must(x => x == null || !string.IsNullOrWhiteSpace(x))
                     .WithMessage("Reporter name must be a valid string.");

                RuleFor(x => x.ReporterContact)
                    .Must(x => x == null || !string.IsNullOrWhiteSpace(x))
                    .WithMessage("Reporter contact must be a valid string.");
            }

            private bool BeValidIncidentType(string type)
            {
                return Enum.TryParse<IncidentType>(type, true, out _);
            }

            private bool BeValidSeverity(string severity)
            {
                return Enum.TryParse<Enums.Severity>(severity, true, out _);
            }

            private bool BeValidStatus(string status)
            {
                return Enum.TryParse<Enums.Status>(status, true, out _);
            }
        }

        public class Handler : IRequestHandler<Command, Unit>
        {
            private readonly IncidentSvc _incidentService;
            private readonly IMapper _mapper;
            private readonly IValidator<Command> _validator;
            private readonly IIncidentEventPublisher _eventPublisher;
            private readonly IS3Service _s3Service;

            public Handler(
                IncidentSvc incidentService, 
                IMapper mapper, 
                IValidator<Command> validator,
                IIncidentEventPublisher eventPublisher,
                IS3Service s3Service)
            {
                _incidentService = incidentService;
                _mapper = mapper;
                _validator = validator;
                _eventPublisher = eventPublisher;
                _s3Service = s3Service;
            }

            public async Task<Unit> Handle(Command request, CancellationToken cancellationToken)
            {
                var validationResult = await _validator.ValidateAsync(request, cancellationToken);
                if (!validationResult.IsValid)
                {
                    var errors = validationResult.Errors
                        .GroupBy(e => e.PropertyName)
                        .ToDictionary(
                            g => g.Key,
                            g => g.Select(e => e.ErrorMessage).ToArray()
                        );

                    throw new StatusException(
                        HttpStatusCode.BadRequest,
                        "ValidationError",
                        "Validation failed",
                        errors
                    );
                }

                if (request.MediaFiles != null && request.MediaFiles.Any())
                {
                    _incidentService.ValidateMediaFiles(request.MediaFiles);
                }

                var incident = await _incidentService.UpdateIncidentById(request.ID, request, _s3Service, cancellationToken);
                var incidentDto = _mapper.Map<IncidentDTO>(incident);

                _ = Task.Run(async () => await _eventPublisher.PublishIncidentUpdatedAsync(incidentDto), cancellationToken);

                return Unit.Value;
            }
        }
    }
}
