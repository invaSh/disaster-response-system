using DispatchService.Domain;
using Microsoft.EntityFrameworkCore;

namespace DispatchService.Persistance
{
    public class DispatchDbContext : DbContext
    {
        public DispatchDbContext(DbContextOptions<DispatchDbContext> options) : base(options)
        {
        }

        public DbSet<Unit> Units { get; set; }
        public DbSet<DispatchOrder> DispatchOrders { get; set; }
        public DbSet<DispatchAssignment> DispatchAssignments { get; set; }
    }
}
