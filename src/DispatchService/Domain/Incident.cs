using Microsoft.EntityFrameworkCore;

namespace DispatchService.Domain
{
    [Index(nameof(IncidentId), IsUnique = true)]
    public class Incident
    {
        public Guid Id { get; set; }
        public string IncidentId { get; set; } = string.Empty;
        
        public string Title { get; set; } = string.Empty;
        
        public string Type { get; set; } = string.Empty;   
        public string Severity { get; set; } = string.Empty; 
        public string Status { get; set; } = string.Empty; 
        
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        
        public DateTime ReportedAt { get; set; }
        public DateTime LastSyncedAt { get; set; }
        
        public Guid? CreatedByUserId { get; set; }
    }
}
