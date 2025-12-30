// i liston: id, code, type, status (e lehte per lista)

using DispatchService.Enums;

namespace DispatchService.DTOs.Units
{
    public class UnitListItemDTO
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = null!;
        public UnitType Type { get; set; }
        public UnitStatus Status { get; set; }
    }
}
