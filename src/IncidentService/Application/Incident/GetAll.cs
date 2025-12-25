using AutoMapper;
using IncidentService.DTOs.Incidents;
using MediatR;
using IncidentService.Services;

namespace IncidentService.Application.Incident
{
    public class GetAll
    {
        public class Query : IRequest<List<IncidentDTO>>
        {

        }

        public class Handler : IRequestHandler<Query, List<IncidentDTO>>
        {
            private readonly IncidentSvc _incidentSvc;
            private readonly IMapper _mapper;

            public Handler(Services.IncidentSvc incidentSvc, IMapper mapper)
            {
                _incidentSvc = incidentSvc;
                _mapper = mapper;
            }

            public async Task<List<IncidentDTO>> Handle(Query request, CancellationToken cancellationToken)
            {
                var incidents = await _incidentSvc.GetAllIncidents();
                return _mapper.Map<List<IncidentDTO>>(incidents);
            }
        }

    }

}
