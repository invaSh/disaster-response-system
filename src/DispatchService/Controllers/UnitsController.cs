using DispatchService.Application.Unit;
using DispatchService.DTOs.Units;
using DispatchService.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace DispatchService.Controllers
{
    [ApiController]
    [Route("api/units")]
    public class UnitsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public UnitsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        // POST: api/units
        [HttpPost]
        public async Task<UnitDetailsDTO> Create([FromBody] CreateUnitRequestDTO dto)
        {
            var command = new Create.Command
            {
                Code = dto.Code,
                Type = dto.Type,
                Latitude = dto.Latitude,
                Longitude = dto.Longitude
            };

            return await _mediator.Send(command);
        }

        // GET: api/units?type=Ambulance&status=Available
        [HttpGet]
        public async Task<List<UnitListItemDTO>> GetAll([FromQuery] UnitType? type, [FromQuery] UnitStatus? status)
        {
            var query = new GetAll.Query
            {
                Type = type,
                Status = status
            };

            return await _mediator.Send(query);
        }

        // GET: api/units/{id}
        [HttpGet("{id:guid}")]
        public async Task<UnitDetailsDTO> GetOne(Guid id)
        {
            return await _mediator.Send(new GetOne.Query { Id = id });
        }

        // PUT: api/units/{id}
        [HttpPut("{id:guid}")]
        public async Task<UnitDetailsDTO> Update(Guid id, [FromBody] UpdateUnitRequestDTO dto)
        {
            var command = new Update.Command
            {
                Id = id,
                Status = dto.Status,
                Latitude = dto.Latitude,
                Longitude = dto.Longitude
            };

            return await _mediator.Send(command);
        }
    }
}
