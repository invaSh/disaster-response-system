using DispatchService.DTOs.DispatchOrders;
using DispatchService.Helpers;
using DispatchService.Services;
using FluentValidation;
using MediatR;
using System.Net;

namespace DispatchService.Application.DispatchOrder
{
    public class Update
    {
        public class Command : IRequest<DispatchOrderDetailsDTO>
        {
            public Guid Id { get; set; }
            public string? Notes { get; set; }
        }

        public class CommandValidator : AbstractValidator<Command>
        {
            public CommandValidator()
            {
                RuleFor(x => x.Id).NotEmpty().WithMessage("Id is required.");

                RuleFor(x => x.Notes)
                    .MaximumLength(1000).WithMessage("Notes is too long.")
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

                var dto = new UpdateDispatchOrderRequestDTO
                {
                    Notes = request.Notes
                };

                return await _dispatchSvc.UpdateDispatchOrderNotes(request.Id, dto, ct);
            }
        }
    }
}
