using AutoMapper;
using NotificationService.Application.Notifications;
using NotificationService.Domain;
using NotificationService.Persistance;
using NotificationService.Helpers;
using NotificationService.Application.Notification;
using Microsoft.EntityFrameworkCore;

namespace NotificationService.Services
{
    public class NotificationSvc
    {
        private readonly NotificationDbContext _context;
        private readonly IMapper _mapper;

        public NotificationSvc(NotificationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<Notification> CreateNotification(Create.Command request, CancellationToken ct)
        {
            var notification = _mapper.Map<Notification>(request);
            notification.Id = Guid.NewGuid();
            notification.IsRead = false;
            notification.CreatedAt = DateTime.UtcNow;
            notification.Metadata ??= new Dictionary<string, string>();

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync(ct);
            return notification;
        }

        public async Task<Notification> DeleteNotification(Guid id)
        {
            var notification = await _context.Notifications.FirstOrDefaultAsync(n => n.Id == id);
            if (notification == null)
                throw new Exception("Notification not found");

            _context.Notifications.Remove(notification);
            await _context.SaveChangesAsync();
            return notification;
        }

        public async Task<Notification> UpdateNotification(Update.Command request, CancellationToken ct)
        {
            var notification = await _context.Notifications.FirstOrDefaultAsync(n => n.Id == request.Id, ct);
            if (notification == null)
                throw new Exception("Notification not found"); 

            if (request.Title != null) notification.Title = request.Title;
            if (request.Message != null) notification.Message = request.Message;
            if (request.Category != null) notification.Category = request.Category;
            if (request.Type != null) notification.Type = request.Type;
            if (request.Severity != null) notification.Severity = request.Severity;
            if (request.RecipientType != null) notification.RecipientType = request.RecipientType;
            if (request.RecipientId != null) notification.RecipientId = request.RecipientId;
            if (request.ReferenceType != null) notification.ReferenceType = request.ReferenceType;
            if (request.ReferenceId != null) notification.ReferenceId = request.ReferenceId;
            if (request.Metadata != null) notification.Metadata = request.Metadata;

            _context.Notifications.Update(notification);
            await _context.SaveChangesAsync(ct);
            return notification;
        }

    }
}
