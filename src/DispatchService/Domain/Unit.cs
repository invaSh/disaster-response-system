using Microsoft.EntityFrameworkCore;
using DispatchService.Enums;

namespace DispatchService.Domain
{
    [Index(nameof(Code), IsUnique = true)]
    public class Unit
    {
        public Guid Id { get; set; }

        // psh. AMB-01, FIRE-03
        public string Code { get; set; } = null!;

        public UnitType Type { get; set; }

        public UnitStatus Status { get; set; } = UnitStatus.Available;

        // Optional – per tracking ma vone
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        public ICollection<DispatchAssignment> DispatchAssignments { get; set; } = new List<DispatchAssignment>();
    }
}
