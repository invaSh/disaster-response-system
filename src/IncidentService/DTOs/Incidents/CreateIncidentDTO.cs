using IncidentService.Domain;
using IncidentService.DTOs.MediaFile;
using IncidentService.Enums;

namespace IncidentService.DTOs.Incidents
{
    public class CreateIncidentDto
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string Type { get; set; }

        public string ReporterName { get; set; }
        public string ReporterContact { get; set; }

        public double Latitude { get; set; }
        public double Longitude { get; set; }

        public string Severity { get; set; }

        public List<MediaFileDTO> MediaFiles { get; set; }
    }

}
