using IncidentService.DTOs.MediaFile;
using IncidentService.DTOs.Update;
using IncidentService.Enums;

namespace IncidentService.DTOs.Incidents
{
    public class UpdateIncidentDTO
    {
        public string IncidentId { get; set; }
        public string Title { get; set; }
        public string? Description { get; set; }
        public IncidentType Type { get; set; }

        public string? ReporterName { get; set; }
        public string? ReporterContact { get; set; }

        public double Latitude { get; set; }
        public double Longitude { get; set; }

        public DateTime? ResolvedAt { get; set; }
        public string? ResolutionNotes { get; set; }

        public Status Status { get; set; }

        public Severity Severity { get; set; }

        public List<Guid>? AssignedUnits { get; set; }

        public List<UpdateDTO> Updates { get; set; }

        public List<MediaFileDTO> MediaFiles { get; set; }

        public Dictionary<string, string> Metadata { get; set; }
    }

}
