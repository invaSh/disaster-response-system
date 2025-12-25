using AutoMapper;
using FluentValidation;
using IncidentService.DTOs.Incidents;
using IncidentService.DTOs.MediaFile;
using IncidentService.Enums;
using IncidentService.Middlewares;
using IncidentService.Services;
using MediatR;
using System.Net;

namespace IncidentService.Application.Incident
{
    public class Create
    {
        public class Command : IRequest<IncidentDTO>
        {
            public string Title { get; set; }
            public string Description { get; set; }
            public string Type { get; set; }

            public string ReporterName { get; set; }
            public string ReporterContact { get; set; }

            public double Latitude { get; set; }
            public double Longitude { get; set; }

            public string Severity { get; set; }

            public List<MediaFileDTO>? MediaFiles { get; set; }
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

                RuleFor(x => x.Longitude).NotEmpty().WithMessage("Longitude must be between -180 and 180.");

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
        }

        public class Handler : IRequestHandler<Command, IncidentDTO>
        {
            private readonly IncidentSvc _incidentService;
            private readonly IMapper _mapper;
            private readonly IValidator<Command> _validator;

            public Handler(IncidentSvc incidentService, IMapper mapper, IValidator<Command> validator)
            {
                _incidentService = incidentService;
                _mapper = mapper;
                _validator = validator;
            }

            public async Task<IncidentDTO> Handle(Command request, CancellationToken cancellationToken)
            {
                var validationResult = await _validator.ValidateAsync(request, cancellationToken);
                if (!validationResult.IsValid)
                {
                    var errors = validationResult.Errors
                        .GroupBy(e => e.PropertyName)
                        .ToDictionary(
                            g => g.Key,
                            g => g.First().ErrorMessage
                        );
                    throw new StatusException(HttpStatusCode.BadRequest, new
                    {
                        Status = "ValidationFailed",
                        Errors = errors
                    });
                }
                var incident = await _incidentService.CreateIncident(request);

                return _mapper.Map<IncidentDTO>(incident);
            }
        }

    }
}
