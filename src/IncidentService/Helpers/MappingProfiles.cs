using AutoMapper;
using IncidentService.Domain;
using IncidentService.DTOs.Incidents;
using IncidentService.Application.Incident;

namespace IncidentService.Helpers
{
    public class MappingProfiles : Profile
    {
        public MappingProfiles()
        {
            CreateMap<Incident, GetAllInvoicesDTO>();
            CreateMap<CreateIncidentDto, Incident>();
            CreateMap<Incident, IncidentDTO>();
            CreateMap<UpdateIncidentDTO, Incident>();
            CreateMap<Create.Command, Incident>();
        }
    }
}
