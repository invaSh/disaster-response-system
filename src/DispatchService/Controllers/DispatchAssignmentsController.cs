using DispatchService.Application.DispatchAssignment;
using DispatchService.DTOs.DispatchAssignments;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DispatchService.Controllers
{
    [ApiController]
    [Route("api/dispatchorders/{dispatchOrderId:guid}/assignments")]
    [Authorize]
    public class DispatchAssignmentsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public DispatchAssignmentsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        // POST: api/dispatchorders/{dispatchOrderId}/assignments
        [HttpPost]
        [Authorize(Roles = "Admin,DisMan")]
        public async Task<DispatchAssignmentDTO> Create(Guid dispatchOrderId, [FromBody] CreateDispatchAssignmentRequestDTO dto)
        {
            var command = new Create.Command
            {
                DispatchOrderId = dispatchOrderId,
                UnitId = dto.UnitId
            };

            return await _mediator.Send(command);
        }

        // PUT: api/dispatchorders/{dispatchOrderId}/assignments/{assignmentId}/status
        [HttpPut("{assignmentId:guid}/status")]
        [Authorize(Roles = "Admin,DisMan")]
        public async Task<DispatchAssignmentDTO> UpdateStatus(
            Guid dispatchOrderId,
            Guid assignmentId,
            [FromBody] UpdateDispatchAssignmentStatusRequestDTO dto)
        {
            var command = new UpdateStatus.Command
            {
                AssignmentId = assignmentId,
                Status = dto.Status
            };

            return await _mediator.Send(command);
        }
    }
}
