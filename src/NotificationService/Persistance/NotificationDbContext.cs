using Microsoft.EntityFrameworkCore;
using NotificationService.Domain;

namespace NotificationService.Persistance
{
    public class NotificationDbContext : DbContext
    {
        public NotificationDbContext(DbContextOptions<NotificationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Notification> Notifications { get; set; }
    }
}
