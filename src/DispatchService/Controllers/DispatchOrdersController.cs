using DispatchService.Application.DispatchOrder;
using DispatchService.DTOs.DispatchOrders;
using DispatchService.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace DispatchService.Controllers
{
    [ApiController]
    [Route("api/dispatchorders")]
    public class DispatchOrdersController : ControllerBase
    {
        private readonly IMediator _mediator;

        public DispatchOrdersController(IMediator mediator)
        {
            _mediator = mediator;
        }

        // POST: api/dispatchorders
        [HttpPost]
        public async Task<DispatchOrderDetailsDTO> Create([FromBody] CreateDispatchOrderRequestDTO dto)
        {
            var command = new Create.Command
            {
                IncidentId = dto.IncidentId,
                Notes = dto.Notes
            };

            return await _mediator.Send(command);
        }

        // GET: api/dispatchorders?status=InProgress
        [HttpGet]
        public async Task<List<DispatchOrderListItemDTO>> GetAll([FromQuery] DispatchStatus? status)
        {
            return await _mediator.Send(new GetAll.Query { Status = status });
        }

        // GET: api/dispatchorders/{id}
        [HttpGet("{id:guid}")]
        public async Task<DispatchOrderDetailsDTO> GetOne(Guid id)
        {
            return await _mediator.Send(new GetOne.Query { Id = id });
        }

        // GET: api/dispatchorders/by-incident/{incidentId}
        [HttpGet("by-incident/{incidentId:guid}")]
        public async Task<DispatchOrderDetailsDTO> GetByIncidentId(Guid incidentId)
        {
            return await _mediator.Send(new GetByIncidentId.Query { IncidentId = incidentId });
        }

        // PUT: api/dispatchorders/{id} 
        [HttpPut("{id:guid}")]
        public async Task<DispatchOrderDetailsDTO> UpdateNotes(Guid id, [FromBody] UpdateDispatchOrderRequestDTO dto)
        {
            var command = new Update.Command
            {
                Id = id,
                Notes = dto.Notes
            };

            return await _mediator.Send(command);
        }
    }
}
