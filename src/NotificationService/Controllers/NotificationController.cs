using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NotificationService.Application.Notifications;
using NotificationService.Application.Notification;
using NotificationService.DTOs.NotificationService.DTOs;

namespace NotificationService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class NotificationController : ControllerBase
    {
        private readonly IMediator _mediator;
        public NotificationController(IMediator mediator)
        {
            _mediator = mediator;
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
    }
}
