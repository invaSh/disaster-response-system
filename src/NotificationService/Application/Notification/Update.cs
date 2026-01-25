using AutoMapper;
using FluentValidation;
using MediatR;
using NotificationService.DTOs.NotificationService.DTOs;
using NotificationService.Helpers;
using NotificationService.Services;
using System.Net;

namespace NotificationService.Application.Notifications
{
    public class Update
    {
        public class Command : IRequest<NotificationDTO>
        {
            public Guid Id { get; set; } 
            public string? Title { get; set; }
            public string? Message { get; set; }
            public string? Category { get; set; }
            public string? Type { get; set; }
            public string? Severity { get; set; }
            public string? RecipientType { get; set; }
            public string? RecipientId { get; set; }
            public string? ReferenceType { get; set; }
            public string? ReferenceId { get; set; }
            public Dictionary<string, string>? Metadata { get; set; }
        }

        public class CommandValidator : AbstractValidator<Command>
        {
            public CommandValidator()
            {
                RuleFor(x => x.Id)
                    .NotEmpty().WithMessage("Id is required.");

                RuleFor(x => x)
                    .Must(x => IsReferencePairValid(x.ReferenceType, x.ReferenceId))
                    .WithMessage("ReferenceType and ReferenceId must be provided together (both or none).");

                RuleForEach(x => x.Metadata)
                    .Must(kv => !string.IsNullOrWhiteSpace(kv.Key) && kv.Value != null)
                    .When(x => x.Metadata != null)
                    .WithMessage("Metadata keys must not be empty and values must not be null.");
            }

            private bool IsReferencePairValid(string? referenceType, string? referenceId)
            {
                var hasType = !string.IsNullOrWhiteSpace(referenceType);
                var hasId = !string.IsNullOrWhiteSpace(referenceId);
                return (hasType && hasId) || (!hasType && !hasId);
            }
        }

        public class Handler : IRequestHandler<Command, NotificationDTO>
        {
            private readonly NotificationSvc _notificationService;
            private readonly IMapper _mapper;
            private readonly IValidator<Command> _validator;

            public Handler(NotificationSvc notificationService, IMapper mapper, IValidator<Command> validator)
            {
                _notificationService = notificationService;
                _mapper = mapper;
                _validator = validator;
            }

            public async Task<NotificationDTO> Handle(Command request, CancellationToken cancellationToken)
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
                        System.Net.HttpStatusCode.BadRequest,
                        "ValidationError",
                        "Validation failed",
                        errors
                    );
                }

                var updatedNotification = await _notificationService.UpdateNotification(request, cancellationToken);
                return _mapper.Map<NotificationDTO>(updatedNotification);
            }
        }
    }
}
