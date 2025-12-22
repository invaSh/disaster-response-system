using IncidentService.Domain;
using Microsoft.EntityFrameworkCore;

namespace IncidentService.Persistance
{
    public class IncidentDbContext : DbContext
    {
        public IncidentDbContext(DbContextOptions<IncidentDbContext> options) : base(options)
        {
        }

        public DbSet<Incident> Incidents { get; set; }
        public DbSet<Update> Updates { get; set; }
        public DbSet<MediaFile> MediaFiles { get; set; }
    }
}
