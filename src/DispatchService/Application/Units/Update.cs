using DispatchService.DTOs.Units;
using DispatchService.Helpers;
using DispatchService.Services;
using FluentValidation;
using MediatR;
using System.Net;

namespace DispatchService.Application.Unit
{
    public class Update
    {
        public class Command : IRequest<UnitDetailsDTO>
        {
            public Guid Id { get; set; }
            public Enums.UnitStatus Status { get; set; }
            public double? Latitude { get; set; }
            public double? Longitude { get; set; }
        }

        public class CommandValidator : AbstractValidator<Command>
        {
            public CommandValidator()
            {
                RuleFor(x => x.Id).NotEmpty().WithMessage("Id is required.");

                RuleFor(x => x.Latitude)
                    .InclusiveBetween(-90, 90)
                    .When(x => x.Latitude.HasValue)
                    .WithMessage("Latitude must be between -90 and 90.");

                RuleFor(x => x.Longitude)
                    .InclusiveBetween(-180, 180)
                    .When(x => x.Longitude.HasValue)
                    .WithMessage("Longitude must be between -180 and 180.");
            }
        }

        public class Handler : IRequestHandler<Command, UnitDetailsDTO>
        {
            private readonly DispatchSvc _dispatchSvc;
            private readonly IValidator<Command> _validator;

            public Handler(DispatchSvc dispatchSvc, IValidator<Command> validator)
            {
                _dispatchSvc = dispatchSvc;
                _validator = validator;
            }

            public async Task<UnitDetailsDTO> Handle(Command request, CancellationToken ct)
            {
                var validationResult = await _validator.ValidateAsync(request, ct);
                if (!validationResult.IsValid)
                {
                    var errors = validationResult.Errors
                        .GroupBy(e => e.PropertyName)
                        .ToDictionary(g => g.Key, g => g.First().ErrorMessage);

                    throw new StatusException(HttpStatusCode.BadRequest, "ValidationError", "Validation failed", errors);
                }

                var dto = new UpdateUnitRequestDTO
                {
                    Status = request.Status,
                    Latitude = request.Latitude,
                    Longitude = request.Longitude
                };

                return await _dispatchSvc.UpdateUnit(request.Id, dto, ct);
            }
        }
    }
}
