using DispatchService.DTOs.Units;
using DispatchService.Helpers;
using DispatchService.Services;
using FluentValidation;
using MediatR;
using System.Net;

namespace DispatchService.Application.Unit
{
    public class GetOne
    {
        public class Query : IRequest<UnitDetailsDTO>
        {
            public Guid Id { get; set; }
        }

        public class QueryValidator : AbstractValidator<Query>
        {
            public QueryValidator()
            {
                RuleFor(x => x.Id).NotEmpty().WithMessage("Id is required.");
            }
        }

        public class Handler : IRequestHandler<Query, UnitDetailsDTO>
        {
            private readonly DispatchSvc _dispatchSvc;
            private readonly IValidator<Query> _validator;

            public Handler(DispatchSvc dispatchSvc, IValidator<Query> validator)
            {
                _dispatchSvc = dispatchSvc;
                _validator = validator;
            }

            public async Task<UnitDetailsDTO> Handle(Query request, CancellationToken ct)
            {
                var validationResult = await _validator.ValidateAsync(request, ct);
                if (!validationResult.IsValid)
                {
                    var errors = validationResult.Errors
                        .GroupBy(e => e.PropertyName)
                        .ToDictionary(g => g.Key, g => g.First().ErrorMessage);

                    throw new StatusException(HttpStatusCode.BadRequest, "ValidationError", "Validation failed", errors);
                }

                return await _dispatchSvc.GetUnitById(request.Id, ct);
            }
        }
    }
}
