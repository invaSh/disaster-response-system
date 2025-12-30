// perditesim te notes (status veq te business layer perditesohet)

namespace DispatchService.DTOs.DispatchOrders
{
    public class UpdateDispatchOrderRequestDTO
    {
        public string? Notes { get; set; }
        public DateTime? CompletedAt { get; set; }
    }
}
