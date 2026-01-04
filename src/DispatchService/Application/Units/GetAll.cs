using DispatchService.DTOs.Units;
using DispatchService.Services;
using MediatR;

namespace DispatchService.Application.Unit
{
    public class GetAll
    {
        public class Query : IRequest<List<UnitListItemDTO>>
        {
            public Enums.UnitType? Type { get; set; }
            public Enums.UnitStatus? Status { get; set; }
        }

        public class Handler : IRequestHandler<Query, List<UnitListItemDTO>>
        {
            private readonly DispatchSvc _dispatchSvc;

            public Handler(DispatchSvc dispatchSvc)
            {
                _dispatchSvc = dispatchSvc;
            }

            public async Task<List<UnitListItemDTO>> Handle(Query request, CancellationToken ct)
            {
                return await _dispatchSvc.GetUnits(request.Type, request.Status, ct);
            }
        }
    }
}
