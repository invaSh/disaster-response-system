using DispatchService.DTOs.DispatchAssignments;
using DispatchService.Helpers;
using DispatchService.Services;
using FluentValidation;
using MediatR;
using System.Net;

namespace DispatchService.Application.DispatchAssignment
{
    public class UpdateStatus
    {
        public class Command : IRequest<DispatchAssignmentDTO>
        {
            public Guid AssignmentId { get; set; }
            public Enums.AssignmentStatus Status { get; set; }
        }

        public class CommandValidator : AbstractValidator<Command>
        {
            public CommandValidator()
            {
                RuleFor(x => x.AssignmentId)
                    .NotEmpty().WithMessage("AssignmentId is required.");
            }
        }

        public class Handler : IRequestHandler<Command, DispatchAssignmentDTO>
        {
            private readonly DispatchSvc _dispatchSvc;
            private readonly IValidator<Command> _validator;

            public Handler(DispatchSvc dispatchSvc, IValidator<Command> validator)
            {
                _dispatchSvc = dispatchSvc;
                _validator = validator;
            }

            public async Task<DispatchAssignmentDTO> Handle(Command request, CancellationToken ct)
            {
                var validationResult = await _validator.ValidateAsync(request, ct);
                if (!validationResult.IsValid)
                {
                    var errors = validationResult.Errors
                        .GroupBy(e => e.PropertyName)
                        .ToDictionary(g => g.Key, g => g.First().ErrorMessage);

                    throw new StatusException(HttpStatusCode.BadRequest, "ValidationError", "Validation failed", errors);
                }

                var dto = new UpdateDispatchAssignmentStatusRequestDTO
                {
                    Status = request.Status
                };

                return await _dispatchSvc.UpdateDispatchAssignmentStatus(request.AssignmentId, dto, ct);
            }
        }
    }
}
