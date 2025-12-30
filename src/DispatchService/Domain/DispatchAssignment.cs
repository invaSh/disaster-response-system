using DispatchService.Enums;

namespace DispatchService.Domain
{
    public class DispatchAssignment
    {
        public Guid Id { get; set; }

        public Guid DispatchOrderId { get; set; }
        public DispatchOrder DispatchOrder { get; set; } = null!;

        public Guid UnitId { get; set; }
        public Unit Unit { get; set; } = null!;

        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

        public AssignmentStatus Status { get; set; } = AssignmentStatus.Assigned;
    }
}
