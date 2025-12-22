using IncidentService.Domain;
using IncidentService.Enums;

namespace IncidentService.DTOs.Incidents
{
    public class GetAllInvoicesDTO
    {
        public Guid ID { get; set; }
        public string IncidentId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public IncidentType Type { get; set; }

        public double Latitude { get; set; }
        public double Longitude { get; set; }

        public DateTime ReportedAt { get; set; } = DateTime.UtcNow;

        public Status Status { get; set; }
        public Severity Severity { get; set; }
    }
}
