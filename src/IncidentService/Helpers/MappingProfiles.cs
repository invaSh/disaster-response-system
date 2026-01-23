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
            CreateMap<Incident, IncidentDTO>()
                .ForMember(dest => dest.ID, opt => opt.MapFrom(src => src.Id.ToString()))
                .ForMember(dest => dest.CreatedByUserId, opt => opt.MapFrom(src => src.CreatedByUserId))
                .ForMember(dest => dest.MediaFiles, opt => opt.MapFrom(src => src.MediaFiles != null ? src.MediaFiles.ToList() : new List<MediaFile>()));
            CreateMap<Create.Command, Incident>()
                .ForMember(dest => dest.MediaFiles, opt => opt.Ignore()); // MediaFiles are handled separately
                // CreatedByUserId is automatically mapped by convention (same property name and type)
            CreateMap<Application.Incident.Update.Command, Incident>()
                .ForMember(dest => dest.MediaFiles, opt => opt.Ignore()); // MediaFiles are handled separately
            CreateMap<MediaFile, MediaFileDTO>()
                .ForMember(dest => dest.ID, opt => opt.MapFrom(src => src.ID.ToString()));
        }
    }
}
