//listohen te dhenat kryesore
using DispatchService.Enums;

namespace DispatchService.DTOs.DispatchOrders
{
    public class DispatchOrderListItemDTO
    {
        public Guid Id { get; set; }
        public Guid IncidentId { get; set; }

        public DispatchStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }

        public int AssignmentsCount { get; set; }

        public List<string> Notes { get; set; } = new List<string>();
    }
}
