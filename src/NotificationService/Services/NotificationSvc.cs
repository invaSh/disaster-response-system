using AutoMapper;
using NotificationService.Application.Notifications;
using NotificationService.Domain;
using NotificationService.Persistance;
using NotificationService.Helpers;
using NotificationService.Application.Notification;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Net;

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

        private async Task<string> GetIncidentSeverityAsync(string incidentReferenceId, CancellationToken ct)
        {
            // We cannot use Dictionary.ContainsKey / indexer in an EF query here (not translatable).
            // So we load a small set of notifications for this incident and filter metadata in-memory.
            var notifications = await _context.Notifications
                .AsNoTracking()
                .Where(n => n.ReferenceType == "Incident" && n.ReferenceId == incidentReferenceId)
                .OrderByDescending(n => n.CreatedAt)
                .Select(n => new { n.Severity, n.CreatedAt, n.Metadata })
                .ToListAsync(ct);

            var createdSeverity = notifications
                .Where(n => n.Metadata != null &&
                            n.Metadata.TryGetValue("EventType", out var evt) &&
                            evt == "IncidentCreated")
                .Select(n => n.Severity)
                .FirstOrDefault(s => !string.IsNullOrWhiteSpace(s));

            if (!string.IsNullOrWhiteSpace(createdSeverity))
                return createdSeverity;

            var anySeverity = notifications
                .Select(n => n.Severity)
                .FirstOrDefault(s => !string.IsNullOrWhiteSpace(s));

            return string.IsNullOrWhiteSpace(anySeverity) ? "Low" : anySeverity;
        }

        public async Task<Notification> CreateNotification(Create.Command request, CancellationToken ct)
        {
            try
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
            catch (StatusException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw HelperService.MapToStatusException(ex);
            }
        }

        // Event-driven helper: Incident created -> notify the incident creator user
        public async Task<Notification> CreateIncidentCreatedNotification(
            Guid createdByUserId,
            string incidentDbId,
            string incidentPublicId,
            string? incidentStatus,
            string? severity,
            double latitude,
            double longitude,
            CancellationToken ct)
        {
            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                Title = "Incident",
                Message = "u krijua incidenti me sukses",
                Category = "Incident",
                Type = "Info",
                Severity = string.IsNullOrWhiteSpace(severity) ? "Low" : severity,
                RecipientType = "User",
                RecipientId = createdByUserId.ToString(),
                ReferenceType = "Incident",
                ReferenceId = incidentDbId ?? string.Empty,
                IsRead = false,
                CreatedAt = DateTime.UtcNow,
                Metadata = new Dictionary<string, string>
                {
                    { "EventType", "IncidentCreated" },
                    { "IncidentId", incidentPublicId ?? string.Empty },
                    { "IncidentStatus", incidentStatus ?? string.Empty },
                    { "CreatedByUserId", createdByUserId.ToString() },
                    { "Latitude", latitude.ToString(CultureInfo.InvariantCulture) },
                    { "Longitude", longitude.ToString(CultureInfo.InvariantCulture) }
                }
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync(ct);
            return notification;
        }

        // API helper: no payload, caller provides incidentId, we notify current user.
        public async Task<Notification> CreateIncidentSeenNotification(
            Guid incidentId,
            Guid userId,
            CancellationToken ct)
        {
            var severity = await GetIncidentSeverityAsync(incidentId.ToString(), ct);
            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                Title = "Incident",
                Message = "Incidenti qe keni krijuar eshte pare",
                Category = "Incident",
                Type = "Info",
                Severity = severity,
                RecipientType = "User",
                RecipientId = userId.ToString(),
                ReferenceType = "Incident",
                ReferenceId = incidentId.ToString(),
                IsRead = false,
                CreatedAt = DateTime.UtcNow,
                Metadata = new Dictionary<string, string>
                {
                    { "EventType", "IncidentSeen" },
                    { "IncidentId", incidentId.ToString() },
                    { "UserId", userId.ToString() }
                }
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync(ct);
            return notification;
        }

        public async Task<Notification> CreateDispatchNotification(
            Guid createdByUserId,
            Guid incidentId,
            string dispatchEventType,
            Guid? dispatchOrderId,
            Guid? dispatchAssignmentId,
            CancellationToken ct)
        {
            var severity = await GetIncidentSeverityAsync(incidentId.ToString(), ct);
            var message = dispatchEventType switch
            {
                "DispatchOrderCreated" => "incidenti eshte pare nga ekipet e ndihmes",
                "DispatchAssignmentCreated" => "ekipa jane rruges",
                "DispatchAssignmentCompleted" => "u morrem me incident flm klm",
                _ => "Dispatch update"
            };

            var meta = new Dictionary<string, string>
            {
                { "EventType", dispatchEventType },
                { "IncidentId", incidentId.ToString() }
            };

            if (dispatchOrderId.HasValue) meta["DispatchOrderId"] = dispatchOrderId.Value.ToString();
            if (dispatchAssignmentId.HasValue) meta["DispatchAssignmentId"] = dispatchAssignmentId.Value.ToString();

            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                Title = "Incident",
                Message = message,
                Category = "Dispatch",
                Type = "Info",
                Severity = severity,
                RecipientType = "User",
                RecipientId = createdByUserId.ToString(),
                ReferenceType = "Incident",
                ReferenceId = incidentId.ToString(),
                IsRead = false,
                CreatedAt = DateTime.UtcNow,
                Metadata = meta
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync(ct);
            return notification;
        }

        public async Task<Notification> DeleteNotification(Guid id)
        {
            try
            {
                var notification = await _context.Notifications.FirstOrDefaultAsync(n => n.Id == id);
                if (notification == null)
                    throw new StatusException(HttpStatusCode.NotFound, "NotFound", "Notification not found.", new { Id = "Notification does not exist." });

                _context.Notifications.Remove(notification);
                await _context.SaveChangesAsync();
                return notification;
            }
            catch (StatusException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw HelperService.MapToStatusException(ex);
            }
        }

        public async Task<Notification> UpdateNotification(Update.Command request, CancellationToken ct)
        {
            try
            {
                var notification = await _context.Notifications.FirstOrDefaultAsync(n => n.Id == request.Id, ct);
                if (notification == null)
                    throw new StatusException(HttpStatusCode.NotFound, "NotFound", "Notification not found.", new { Id = "Notification does not exist." });

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
            catch (StatusException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw HelperService.MapToStatusException(ex);
            }
        }

    }
}
