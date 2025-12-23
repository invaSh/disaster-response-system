using AutoMapper;
using IncidentService.Domain;
using IncidentService.DTOs.Incidents;
using IncidentService.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace IncidentService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class IncidentController : ControllerBase
    {
        private readonly IncidentSvc _incidentService;
        private readonly IMapper _mapper;

        public IncidentController(IncidentSvc incidentService, IMapper mapper)
        {
            _incidentService = incidentService;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllIncidents()
        {
            var incidents = await _incidentService.GetAllIncidents();
            return Ok(_mapper.Map<List<GetAllInvoicesDTO>>(incidents));
        }

        [HttpPost]
        public async Task<IActionResult> CreateIncident([FromBody] CreateIncidentDto createIncidentDto)
        {
            var incident = await _incidentService.CreateIncident(createIncidentDto);
            var result = _mapper.Map<IncidentDTO>(incident);
            return CreatedAtAction(nameof(GetIncidentById), new { id = incident.Id }, result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetIncidentById(Guid id)
        {
            var incident = await _incidentService.GetIncidentById(id);
            if (incident == null)
            {
                return NotFound();
            }
            return Ok(_mapper.Map<IncidentDTO>(incident));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateIncidentById(Guid id, [FromBody] UpdateIncidentDTO updateIncidentDto)
        {
            var updatedIncident = await _incidentService.UpdateIncidentById(id, updateIncidentDto);
            return Ok(_mapper.Map<IncidentDTO>(updatedIncident));
        }
        
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteIncident(Guid id)
        {
            var result = await _incidentService.DeleteIncident(id);
            if (result == null) return BadRequest("Something went wrong.");
            return Ok("Incident deleted successfully.");
        }
    }
}
