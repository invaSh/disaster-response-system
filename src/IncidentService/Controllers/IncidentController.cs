using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using IncidentService.Application.Incident;
using IncidentService.DTOs.Incidents;

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
        public async Task<List<IncidentDTO>> GetAllIncidents()
        {
            return await _mediator.Send(new GetAll.Query());
        }

        [HttpPost]
        public async Task<IncidentDTO> CreateIncident([FromBody] Create.Command command)
        {
            return await _mediator.Send(command);
        }



  
    }
}
