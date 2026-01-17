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
            CreateMap<Create.Command, Incident>()
                .ForMember(dest => dest.MediaFiles, opt => opt.Ignore()); // MediaFiles are handled separately
            CreateMap<Application.Incident.Update.Command, Incident>();
            CreateMap<MediaFile, MediaFileDTO>()
                .ForMember(dest => dest.ID, opt => opt.MapFrom(src => src.ID.ToString()));
        }
    }
}
