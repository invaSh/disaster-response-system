// update statusin dhe location (zakonisht code dhe type nuk do duhej te perditesohen)
using DispatchService.Enums;

namespace DispatchService.DTOs.Units
{
    public class UpdateUnitRequestDTO
    {
        public UnitStatus Status { get; set; }

        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
    }
}
