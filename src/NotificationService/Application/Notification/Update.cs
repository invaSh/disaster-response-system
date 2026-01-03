using MediatR;
using Microsoft.EntityFrameworkCore;
using NotificationService.Helpers;
using NotificationService.Persistance;
using System.Net;

namespace NotificationService.Application.Notification
{
    public class Update
    {
        public class Command : IRequest<Unit>
        {
            public Guid ID { get; set; }

            public string Title { get; set; }
            public string Message { get; set; }
            public string Category { get; set; }
            public string Type { get; set; }
            public string Severity { get; set; }
            public string RecipientType { get; set; }
            public string RecipientId { get; set; }
            public bool IsRead { get; set; }
            public string ReferenceType { get; set; }
            public string ReferenceId { get; set; }
            public Dictionary<string, string> Metadata { get; set; }
        }

        public class Handler : IRequestHandler<Command, Unit>
        {
            private readonly NotificationDbContext _context;

            public Handler(NotificationDbContext context)
            {
                _context = context;
            }

            public async Task<Unit> Handle(Command request, CancellationToken cancellationToken)
            {
                var notification = await _context.Notifications
                    .FirstOrDefaultAsync(x => x.Id == request.ID, cancellationToken);

                if (notification == null)
                {
                    throw new StatusException(
                        HttpStatusCode.NotFound,
                        "NotFound",
                        "Notification not found",
                        new Dictionary<string, string[]>
                        {
                            { "id", new[] { "Notification with this id was not found." } }
                        }
                    );
                }

                // UPDATE FIELDS
                notification.Title = request.Title;
                notification.Message = request.Message;
                notification.Category = request.Category;
                notification.Type = request.Type;
                notification.Severity = request.Severity;
                notification.RecipientType = request.RecipientType;
                notification.RecipientId = request.RecipientId;
                notification.IsRead = request.IsRead;
                notification.ReferenceType = request.ReferenceType;
                notification.ReferenceId = request.ReferenceId;
                notification.Metadata = request.Metadata;

                await _context.SaveChangesAsync(cancellationToken);

                return Unit.Value;
            }
        }
    }
}
