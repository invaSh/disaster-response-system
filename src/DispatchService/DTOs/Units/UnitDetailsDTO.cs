// i liston edhe detajet e tjera (lokacionin)

using DispatchService.Enums;

namespace DispatchService.DTOs.Units
{
    public class UnitDetailsDTO
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = null!;
        public UnitType Type { get; set; }
        public UnitStatus Status { get; set; }

        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
    }
}