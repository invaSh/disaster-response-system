using System.Collections;
using IncidentService.Enums;
using Microsoft.EntityFrameworkCore;

namespace IncidentService.Domain
{
    [Index(nameof(IncidentId), IsUnique = true)]
    public class Incident
    {
        public Guid Id { get; set; }
        public string IncidentId { get; set; }
        public string Title { get; set; }             
        public string? Description { get; set; }        
        public IncidentType Type { get; set; }        

        public string? ReporterName { get; set; }
        public string? ReporterContact { get; set; }  

        public double Latitude { get; set; }
        public double Longitude { get; set; }

        public DateTime ReportedAt { get; set; } = DateTime.UtcNow;     
        public DateTime? ResolvedAt { get; set; }
        public string? ResolutionNotes { get; set; }

        public Status Status { get; set; }     

        public Severity Severity { get; set; } 

        public List<Guid>? AssignedUnits { get; set; } 

        public ICollection<Update> Updates { get; set; } 

        public ICollection<MediaFile>? MediaFiles { get; set; }

        public Dictionary<string, string>? Metadata { get; set; }

        public Guid? CreatedByUserId { get; set; }
    }

}
