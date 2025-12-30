using DispatchService.Domain;
using Microsoft.EntityFrameworkCore;

namespace DispatchService.Persistance
{
    public class DispatchDbContext : DbContext
    {
        public DispatchDbContext(DbContextOptions<DispatchDbContext> options)
            : base(options)
        {
        }

        public DbSet<Unit> Units => Set<Unit>();
        public DbSet<DispatchOrder> DispatchOrders => Set<DispatchOrder>();
        public DbSet<DispatchAssignment> DispatchAssignments => Set<DispatchAssignment>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Unit
            modelBuilder.Entity<Unit>(entity =>
            {
                entity.HasKey(u => u.Id);

                entity.Property(u => u.Code)
                      .IsRequired()
                      .HasMaxLength(50);

                entity.Property(u => u.Type).IsRequired();
                entity.Property(u => u.Status).IsRequired();

                entity.Property(u => u.Latitude);
                entity.Property(u => u.Longitude);

                entity.HasMany(u => u.DispatchAssignments)
                      .WithOne(a => a.Unit)
                      .HasForeignKey(a => a.UnitId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // DispatchOrder
            modelBuilder.Entity<DispatchOrder>(entity =>
            {
                entity.HasKey(o => o.Id);

                entity.Property(o => o.IncidentId).IsRequired();
                entity.Property(o => o.Status).IsRequired();
                entity.Property(o => o.CreatedAt).IsRequired();

                entity.Property(o => o.Notes)
                      .HasMaxLength(500);

                entity.HasMany(o => o.Assignments)
                      .WithOne(a => a.DispatchOrder)
                      .HasForeignKey(a => a.DispatchOrderId)
                      .OnDelete(DeleteBehavior.Restrict); 
            });

            // DispatchAssignment
            modelBuilder.Entity<DispatchAssignment>(entity =>
            {
                entity.HasKey(a => a.Id);

                entity.Property(a => a.AssignedAt).IsRequired();
                entity.Property(a => a.Status).IsRequired();

                entity.HasIndex(a => a.DispatchOrderId);
                entity.HasIndex(a => a.UnitId);

            });
        }
    }
}
