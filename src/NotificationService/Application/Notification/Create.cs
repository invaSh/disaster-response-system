using AutoMapper;
using FluentValidation;
using MediatR;
using NotificationService.DTOs.NotificationService.DTOs;
using NotificationService.Middlewares;
using NotificationService.Helpers;
using NotificationService.Services;      
using System.Net;

namespace NotificationService.Application.Notifications
{
    public class Create
    {
        public class Command : IRequest<NotificationDTO>
        {
            public string Title { get; set; }
            public string Message { get; set; }

            public string Category { get; set; }      
            public string Type { get; set; }          
            public string Severity { get; set; }     

            public string RecipientType { get; set; } 
            public string RecipientId { get; set; }   

            public string ReferenceType { get; set; } 
            public string ReferenceId { get; set; }   

            public Dictionary<string, string>? Metadata { get; set; }
        }

        public class CommandValidator : AbstractValidator<Command>
        {
            public CommandValidator()
            {
                RuleFor(x => x.Title)
                    .NotEmpty().WithMessage("Title is required.");

                RuleFor(x => x.Message)
                    .NotEmpty().WithMessage("Message is required.");

                RuleFor(x => x.Category)
                    .NotEmpty().WithMessage("Category is required.");

                RuleFor(x => x.Type)
                    .NotEmpty().WithMessage("Type is required.");

                RuleFor(x => x.Severity)
                    .NotEmpty().WithMessage("Severity is required.");

                RuleFor(x => x.RecipientType)
                    .NotEmpty().WithMessage("RecipientType is required.");

                RuleFor(x => x.RecipientId)
                    .NotEmpty().WithMessage("RecipientId is required.")
                    .When(x => !string.Equals(x.RecipientType, "Broadcast", StringComparison.OrdinalIgnoreCase));

                RuleFor(x => x.ReferenceType)
                    .Must(x => string.IsNullOrWhiteSpace(x) || !string.IsNullOrWhiteSpace(x))
                    .WithMessage("ReferenceType must be a valid string.");

                RuleFor(x => x.ReferenceId)
                    .Must(x => string.IsNullOrWhiteSpace(x) || !string.IsNullOrWhiteSpace(x))
                    .WithMessage("ReferenceId must be a valid string.");

                RuleFor(x => x)
                    .Must(x => IsReferencePairValid(x.ReferenceType, x.ReferenceId))
                    .WithMessage("ReferenceType and ReferenceId must be provided together (both or none).");

                RuleForEach(x => x.Metadata)
                    .Must(kv => !string.IsNullOrWhiteSpace(kv.Key) && kv.Value != null)
                    .When(x => x.Metadata != null)
                    .WithMessage("Metadata keys must not be empty and values must not be null.");
            }

            private bool IsReferencePairValid(string referenceType, string referenceId)
            {
                var hasType = !string.IsNullOrWhiteSpace(referenceType);
                var hasId = !string.IsNullOrWhiteSpace(referenceId);
                return (hasType && hasId) || (!hasType && !hasId);
            }
        }

        public class Handler : IRequestHandler<Command, NotificationDTO>
        {
            private readonly NotificationSvc _notificationService;
            private readonly IEmailSender _emailSender;
            private readonly IMapper _mapper;
            private readonly IValidator<Command> _validator;

            public Handler(
                NotificationSvc notificationService,
                IEmailSender emailSender,
                IMapper mapper,
                IValidator<Command> validator)
            {
                _notificationService = notificationService;
                _emailSender = emailSender;
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
                        .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

                    throw new StatusException(HttpStatusCode.BadRequest, "ValidationError", "Validation failed", errors);
                }

                // 1) ruaje ne DB
                var notification = await _notificationService.CreateNotification(request, cancellationToken);

                // 2) TEST: dergo email te TI (per momentin)
                await _emailSender.SendAsync(
                    "festimdibrani9@gmail.com",
                    request.Title,
                    request.Message,
                    cancellationToken
                );

                return _mapper.Map<NotificationDTO>(notification);
            }
        }

    }
}
