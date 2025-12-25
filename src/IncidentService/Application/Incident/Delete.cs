using IncidentService.Services;
using MediatR;

namespace IncidentService.Application.Incident
{
    public class Delete
    {
        public class Command : IRequest<Unit>
        {
            public Guid ID { get; set; }
        }

        public class Handler : IRequestHandler<Command, Unit>
        {
            private readonly IncidentSvc _incidentService;

            public Handler(IncidentSvc incidentSvc)
            {
                _incidentService = incidentSvc;
            }

            public async Task<Unit> Handle(Command request, CancellationToken cancellationToken)
            {
                await _incidentService.DeleteIncident(request.ID);
                return Unit.Value;
            }
        }
    }
}
