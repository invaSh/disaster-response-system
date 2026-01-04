using DispatchService.DTOs.DispatchOrders;
using DispatchService.Enums;
using DispatchService.Services;
using MediatR;

namespace DispatchService.Application.DispatchOrder
{
    public class GetAll
    {
        public class Query : IRequest<List<DispatchOrderListItemDTO>>
        {
            public DispatchStatus? Status { get; set; }
        }

        public class Handler : IRequestHandler<Query, List<DispatchOrderListItemDTO>>
        {
            private readonly DispatchSvc _dispatchSvc;

            public Handler(DispatchSvc dispatchSvc)
            {
                _dispatchSvc = dispatchSvc;
            }

            public async Task<List<DispatchOrderListItemDTO>> Handle(Query request, CancellationToken ct)
            {
                return await _dispatchSvc.GetDispatchOrders(request.Status, ct);
            }
        }
    }
}
