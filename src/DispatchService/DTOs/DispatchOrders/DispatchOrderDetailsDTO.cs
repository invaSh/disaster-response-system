// si list item edhe perfshihen edhe detajet

using DispatchService.DTOs.DispatchAssignments;
using DispatchService.Enums;

namespace DispatchService.DTOs.DispatchOrders
{
    public class DispatchOrderDetailsDTO
    {
        public Guid Id { get; set; }
        public Guid IncidentId { get; set; }

        public DispatchStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }

        public List<string> Notes { get; set; } = new List<string>();

        // order me assignments brenda (view i details)
        public List<DispatchAssignmentDTO> Assignments { get; set; } = new();
    }
}
