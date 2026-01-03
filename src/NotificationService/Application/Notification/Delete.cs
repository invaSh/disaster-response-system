using MediatR;
using NotificationService.Services;

namespace NotificationService.Application.Notification
{
    public class Delete
    {
        public class Command : IRequest<Unit>
        {
            public Guid ID { get; set; }
        }

        public class Handler : IRequestHandler<Command, Unit>
        {
            private readonly NotificationSvc _notificationService;
            public Handler(NotificationSvc notificationSvc)
            {
                _notificationService = notificationSvc;
            }
            public async Task<Unit> Handle(Command request, CancellationToken cancellationToken)
            {
                await _notificationService.DeleteNotification(request.ID);
                return Unit.Value;
            }
        }
    }
}
