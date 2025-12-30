// client dergon incident id, notes (status dhe createdat jane server-generated)

namespace DispatchService.DTOs.DispatchOrders
{
    public class CreateDispatchOrderRequestDTO
    {
        public Guid IncidentId { get; set; }
        public string? Notes { get; set; }
    }
}