using AutoMapper;
using IncidentService.DTOs;
using IncidentService.Services;
using MediatR;

namespace IncidentService.Application.Incident
{
    public class GetOne
    {
        public class Query : IRequest<IncidentDTO>
        {
            public Guid ID { get; set; }
        }

        public class Handler : IRequestHandler<Query, IncidentDTO>
        {
            private readonly IncidentSvc _incidentSvc;
            private readonly IMapper _mapper;

            public Handler(IncidentSvc incidentSvc, IMapper mapper)
            {
                _incidentSvc = incidentSvc;
                _mapper = mapper;
            }

            public async Task<IncidentDTO> Handle(Query request, CancellationToken cancellationToken)
            {
                var incident = await _incidentSvc.GetIncidentById(request.ID);
                return _mapper.Map<IncidentDTO>(incident);
            }
        }
    }
}
