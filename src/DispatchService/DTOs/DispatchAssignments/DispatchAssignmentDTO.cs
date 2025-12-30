using DispatchService.Enums;

namespace DispatchService.DTOs.DispatchAssignments
{
    public class DispatchAssignmentDTO
    {
        public Guid Id { get; set; }

        public Guid DispatchOrderId { get; set; }

        public Guid UnitId { get; set; }
        public string UnitCode { get; set; } = null!;

        public DateTime AssignedAt { get; set; }
        public AssignmentStatus Status { get; set; }
    }
}
