using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using IncidentService.Application.Incident;
using IncidentService.DTOs;
using System.Security.Claims;

namespace IncidentService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class IncidentController : ControllerBase
    {
        private readonly IMediator _mediator;

        public IncidentController(IMediator mediator)
        {
            _mediator = mediator;
        }


        [HttpGet]
        [Authorize(Roles = "Admin,IncMan")]
        public async Task<List<IncidentDTO>> GetAll()
        {
            return await _mediator.Send(new GetAll.Query());
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "Admin,IncMan")]
        public async Task<IncidentDTO> GetOne(Guid id)
        {
            return await _mediator.Send(new GetOne.Query { ID = id });
        }

        [HttpPost]
        [Consumes("multipart/form-data")]
        [Authorize(Roles = "Admin,IncMan,User")]
        public async Task<IncidentDTO> Create([FromForm] Create.Command command)
        {
            var userIdClaim = User.FindFirst("sub") 
                ?? User.FindFirst(ClaimTypes.NameIdentifier);
            
            if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
            {
                command.CreatedByUserId = userId;
            }

            return await _mediator.Send(command);
        }

        [HttpPut("{id}")]
        [Consumes("multipart/form-data")]
        [Authorize(Roles = "Admin,IncMan")]
        public async Task<IActionResult> Update(Guid id, [FromForm] Update.Command command)
        {
            command.ID = id;
            await _mediator.Send(command);
            return Ok(new { message = "Incident updated successfully" });
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin,IncMan")]
        public async Task<IActionResult> Delete(Guid id)
        {
            await _mediator.Send(new Delete.Command { ID = id });
            return Ok(new { message = "Incident deleted successfully" });
        }

    }
}
