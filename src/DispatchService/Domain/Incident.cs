using Microsoft.EntityFrameworkCore;

namespace DispatchService.Domain
{
    [Index(nameof(IncidentId), IsUnique = true)]
    public class Incident
    {
        // Identifiers
        public Guid Id { get; set; }
        public string IncidentId { get; set; } = string.Empty;
        
        // Display Info
        public string Title { get; set; } = string.Empty;
        
        // Dispatch Logic
        public string Type { get; set; } = string.Empty;      // Fire, Medical, Police
        public string Severity { get; set; } = string.Empty;  // Critical, High, Medium, Low
        public string Status { get; set; } = string.Empty;    // Active, Resolved, etc.
        
        // Location (critical for dispatch)
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        
        // Timestamps
        public DateTime ReportedAt { get; set; }
        
        // Cache metadata
        public DateTime LastSyncedAt { get; set; }
        
        // User tracking
        public Guid? CreatedByUserId { get; set; }
    }
}
