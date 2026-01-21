// perditesim te notes (status veq te business layer perditesohet)

namespace DispatchService.DTOs.DispatchOrders
{
    public class UpdateDispatchOrderRequestDTO
    {
        public List<string>? Notes { get; set; }
        // public DateTime? CompletedAt { get; set; } - vendoset vetem kur statusi behet completed.
    }
}
