using DispatchService.Enums;
using Microsoft.EntityFrameworkCore;

namespace DispatchService.Domain
{
    [Index(nameof(IncidentId), IsUnique = true)]
    public class DispatchOrder
    {
        public Guid Id { get; set; }

        // Incident nga Incident Service
        public Guid IncidentId { get; set; }

        public DispatchStatus Status { get; set; } = DispatchStatus.Created;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }

        public string? Notes { get; set; }

        public ICollection<DispatchAssignment> Assignments { get; set; } = new List<DispatchAssignment>();
    }
}
