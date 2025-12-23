using AutoMapper;
using IncidentService.Domain;
using IncidentService.DTOs.Incidents;

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
        }
    }
}
