using MediatR;
using Microsoft.EntityFrameworkCore;
using NotificationService.DTOs.NotificationService.DTOs;
using NotificationService.Persistance;

namespace NotificationService.Application.Notification
{
    public class GetAll
    {
        public class Query : IRequest<List<NotificationDTO>>
        {
        }

        public class Handler : IRequestHandler<Query, List<NotificationDTO>>
        {
            private readonly NotificationDbContext _context;

            public Handler(NotificationDbContext context)
            {
                _context = context;
            }

            public async Task<List<NotificationDTO>> Handle(Query request, CancellationToken cancellationToken)
            {
                var notifications = await _context.Notifications
                    .OrderByDescending(x => x.CreatedAt)
                    .Select(x => new NotificationDTO
                    {
                        Id = x.Id,
                        Title = x.Title,
                        Message = x.Message,
                        Category = x.Category,
                        Type = x.Type,
                        Severity = x.Severity,
                        RecipientType = x.RecipientType,
                        RecipientId = x.RecipientId,
                        IsRead = x.IsRead,
                        CreatedAt = x.CreatedAt,
                        ReadAt = x.ReadAt,
                        ReferenceType = x.ReferenceType,
                        ReferenceId = x.ReferenceId,
                        Metadata = x.Metadata
                    })
                    .ToListAsync(cancellationToken);

                return notifications;
            }
        }
    }
}
