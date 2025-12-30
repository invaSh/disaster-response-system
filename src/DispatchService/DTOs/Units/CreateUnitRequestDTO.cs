//Client e dergon: code, type dhe location
using DispatchService.Enums;

namespace DispatchService.DTOs.Units
{
    public class CreateUnitRequestDTO
    {
        public string Code { get; set; } = null!;
        public UnitType Type { get; set; }

        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
    }
}
