using AutoMapper;
using IncidentService.Domain;
using IncidentService.Application.Incident;
using IncidentService.DTOs;

namespace IncidentService.Helpers
{
    public class MappingProfiles : Profile
    {
        public MappingProfiles()
        {
            CreateMap<Incident, IncidentDTO>();
            CreateMap<Create.Command, Incident>();
            CreateMap<Application.Incident.Update.Command, Incident>();
        }
    }
}
