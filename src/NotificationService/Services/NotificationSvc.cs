using AutoMapper;
using NotificationService.Application.Notifications;
using NotificationService.Domain;
using NotificationService.Persistance;
using NotificationService.Helpers;
using Microsoft.EntityFrameworkCore;

namespace NotificationService.Services
{
    public class NotificationSvc
    {
        private readonly NotificationDbContext _context;
        private readonly IMapper mapper;

        public async Task<Notification> CreateNotification(Create.Command request, CancellationToken ct)
        {
            try
            {
                var notification = mapper.Map<Notification>(request);

                notification.Id = Guid.NewGuid();
                notification.IsRead = false;
                notification.CreatedAt = DateTime.UtcNow;
                notification.Metadata ??= new Dictionary<string, string>();

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync(ct);

                return notification; 
            }
            catch (Exception ex)
            {
                throw HelperService.MapToStatusException(ex); 
            }
        }

        public async Task<Notification> DeleteNotification(Guid id)
        {
            try
            {
                var notification = await GetNotificationById(id);
                _context.Notifications.Remove(notification);
                await _context.SaveChangesAsync();
                return notification;
            }
            catch (Exception ex)
            {
                throw HelperService.MapToStatusException(ex);
            }
        }

        public async Task<Notification> GetNotificationById(Guid id)
        {
            var notification = await _context.Notifications.FirstOrDefaultAsync(n => n.Id == id);
            if (notification == null)
            {
                throw new StatusException(
                    System.Net.HttpStatusCode.NotFound,
                    "NotFound",
                    "Notification not found",
                    new Dictionary<string, string[]>()
                );
            }
            return notification;
        }
    }
}
