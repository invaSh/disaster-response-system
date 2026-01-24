using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NotificationService.Application.Notifications;
using NotificationService.Application.Notification;
using NotificationService.DTOs.NotificationService.DTOs;
using NotificationService.Services;
using System.Security.Claims;

namespace NotificationService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class NotificationController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly NotificationSvc _notificationSvc;

        public NotificationController(IMediator mediator, NotificationSvc notificationSvc)
        {
            _mediator = mediator;
            _notificationSvc = notificationSvc;
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<NotificationDTO> Create([FromBody] Create.Command command)
        {
            return await _mediator.Send(command);
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<List<NotificationDTO>>> GetAll()
        {
            var result = await _mediator.Send(new GetAll.Query());
            return Ok(result);
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<NotificationDTO>> GetById(Guid id)
        {
            var result = await _mediator.Send(new GetOne.Query { Id = id });
            return Ok(result);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<NotificationDTO> Update(Guid id, [FromBody] Update.Command command)
        {
            command.Id = id;
            return await _mediator.Send(command);
        }


        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(Guid id)
        {
            await _mediator.Send(new Delete.Command { ID = id });
            return Ok(new { message = "Notification deleted successfully" });
        }

        // No payload endpoint:
        // POST api/notification/{incidentId}
        // Creates a notification for the current user, referencing the incidentId.
        [HttpPost("{incidentId:guid}")]
        [Authorize(Roles = "Admin,IncMan,User")]
        public async Task<IActionResult> CreateForIncident(Guid incidentId, CancellationToken ct)
        {
            var userIdClaim = User.FindFirst("sub")
                ?? User.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                return Unauthorized(new { message = "UserId missing in token" });

            var notification = await _notificationSvc.CreateIncidentSeenNotification(
                incidentId: incidentId,
                userId: userId,
                ct: ct);

            return Ok(new
            {
                notification.Id,
                notification.Title,
                notification.Message,
                notification.RecipientType,
                notification.RecipientId,
                notification.ReferenceType,
                notification.ReferenceId,
                notification.CreatedAt
            });
        }
    }
}
