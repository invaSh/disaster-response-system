using DispatchService.DTOs.DispatchOrders;
using DispatchService.Helpers;
using DispatchService.Services;
using FluentValidation;
using MediatR;
using System.Net;

namespace DispatchService.Application.DispatchOrder
{
    public class Create
    {
        public class Command : IRequest<DispatchOrderDetailsDTO>
        {
            public Guid IncidentId { get; set; }
            public List<string>? Notes { get; set; }
        }

        public class CommandValidator : AbstractValidator<Command>
        {
            public CommandValidator()
            {
                RuleFor(x => x.IncidentId)
                    .NotEmpty().WithMessage("IncidentId is required.");

                RuleForEach(x => x.Notes)
                    .MaximumLength(1000).WithMessage("Each note entry is too long.")
                    .When(x => x.Notes != null);
            }
        }

        public class Handler : IRequestHandler<Command, DispatchOrderDetailsDTO>
        {
            private readonly DispatchSvc _dispatchSvc;
            private readonly IValidator<Command> _validator;

            public Handler(DispatchSvc dispatchSvc, IValidator<Command> validator)
            {
                _dispatchSvc = dispatchSvc;
                _validator = validator;
            }

            public async Task<DispatchOrderDetailsDTO> Handle(Command request, CancellationToken ct)
            {
                var validationResult = await _validator.ValidateAsync(request, ct);
                if (!validationResult.IsValid)
                {
                    var errors = validationResult.Errors
                        .GroupBy(e => e.PropertyName)
                        .ToDictionary(g => g.Key, g => g.First().ErrorMessage);

                    throw new StatusException(HttpStatusCode.BadRequest, "ValidationError", "Validation failed", errors);
                }

                var dto = new CreateDispatchOrderRequestDTO
                {
                    IncidentId = request.IncidentId,
                    Notes = request.Notes
                };

                return await _dispatchSvc.CreateDispatchOrder(dto, ct);
            }
        }
    }
}
