using MediatR;
using Microsoft.EntityFrameworkCore;
using NotificationService.DTOs.NotificationService.DTOs;
using NotificationService.Helpers;
using NotificationService.Persistance;
using System.Net;

namespace NotificationService.Application.Notification
{
    public class GetOne
    {
        public class Query : IRequest<NotificationDTO>
        {
            public Guid Id { get; set; }
        }

        public class Handler : IRequestHandler<Query, NotificationDTO>
        {
            private readonly NotificationDbContext _context;

            public Handler(NotificationDbContext context)
            {
                _context = context;
            }

            public async Task<NotificationDTO> Handle(Query request, CancellationToken cancellationToken)
            {
                var notification = await _context.Notifications
                    .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

                if (notification == null)
                {
                    throw new StatusException(
                        HttpStatusCode.NotFound,
                        "NotFound",
                        "Notification not found",
                        new { Id = "Notification does not exist." }
                    );
                }

                return new NotificationDTO
                {
                    Id = notification.Id,
                    Title = notification.Title,
                    Message = notification.Message,
                    Category = notification.Category,
                    Type = notification.Type,
                    Severity = notification.Severity,
                    RecipientType = notification.RecipientType,
                    RecipientId = notification.RecipientId,
                    IsRead = notification.IsRead,
                    CreatedAt = notification.CreatedAt,
                    ReadAt = notification.ReadAt,
                    ReferenceType = notification.ReferenceType,
                    ReferenceId = notification.ReferenceId,
                    Metadata = notification.Metadata
                };
            }
        }
    }
}
