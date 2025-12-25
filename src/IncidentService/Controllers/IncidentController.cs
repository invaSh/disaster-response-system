using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using IncidentService.Application.Incident;
using IncidentService.DTOs;

namespace IncidentService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class IncidentController : ControllerBase
    {
        private readonly IMediator _mediator;

        public IncidentController(IMediator mediator)
        {
            _mediator = mediator;
        }


        [HttpGet]
        public async Task<List<IncidentDTO>> GetAll()
        {
            return await _mediator.Send(new GetAll.Query());
        }

        [HttpGet("{id}")]
        public async Task<IncidentDTO> GetOne(Guid id)
        {
            return await _mediator.Send(new GetOne.Query { ID = id });
        }

        [HttpPost]
        public async Task<IncidentDTO> Create([FromBody] Create.Command command)
        {
            return await _mediator.Send(command);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] Update.Command command)
        {
            command.ID = id;
            await _mediator.Send(command);
            return Ok(new { message = "Incident updated successfully" });
        }

    }
}
