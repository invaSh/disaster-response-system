using IncidentService.Domain;
using IncidentService.DTOs.MediaFile;
using IncidentService.DTOs.Update;
using IncidentService.Enums;

namespace IncidentService.DTOs.Incidents
{
    public class IncidentDTO
    {
        public string ID { get; set; }
        public string IncidentId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Type { get; set; }

        public string ReporterName { get; set; }
        public string ReporterContact { get; set; }

        public double Latitude { get; set; }
        public double Longitude { get; set; }

        public DateTime ReportedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ResolvedAt { get; set; }
        public string ResolutionNotes { get; set; }

        public string Status { get; set; }

        public string Severity { get; set; }

        public List<string> AssignedUnits { get; set; }

        public List<UpdateDTO> Updates { get; set; }

        public List<MediaFileDTO> MediaFiles { get; set; }

        public Dictionary<string, string> Metadata { get; set; }
    }
}
