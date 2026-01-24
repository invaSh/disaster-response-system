using AutoMapper;
using DispatchService.Domain;
using DispatchService.DTOs.DispatchOrders;
using DispatchService.DTOs.DispatchAssignments;
using DispatchService.DTOs.Units;
using DispatchService.Enums;
using DispatchService.Helpers;
using DispatchService.Persistance;
using DispatchService.Messaging.Publishers;
using Microsoft.EntityFrameworkCore;
using System.Net;

namespace DispatchService.Services
{
    public class DispatchSvc
    {
        private readonly DispatchDbContext _context;
        private readonly IMapper _mapper;
        private readonly IDispatchEventPublisher _eventPublisher;

        public DispatchSvc(DispatchDbContext context, IMapper mapper, IDispatchEventPublisher eventPublisher)
        {
            _context = context;
            _mapper = mapper;
            _eventPublisher = eventPublisher;
        }


        // Units
        public async Task<UnitDetailsDTO> CreateUnit(CreateUnitRequestDTO dto, CancellationToken ct)
        {
            try
            {
                var exists = await _context.Units.AnyAsync(u => u.Code == dto.Code, ct);
                if (exists)
                    throw new StatusException(HttpStatusCode.Conflict, "Duplicate", "Unit code already exists.", new { Code = "Code must be unique." });

                var unit = _mapper.Map<Unit>(dto);
                unit.Id = Guid.NewGuid();
                unit.Status = UnitStatus.Available;

                _context.Units.Add(unit);
                await _context.SaveChangesAsync(ct);

                return _mapper.Map<UnitDetailsDTO>(unit);
            }
            catch (Exception ex)
            {
                throw HelperService.MapToStatusException(ex);
            }
        }

        public async Task<List<UnitListItemDTO>> GetUnits(UnitType? type, UnitStatus? status, CancellationToken ct)
        {
            try
            {
                var query = _context.Units.AsQueryable();

                if (type.HasValue)
                    query = query.Where(u => u.Type == type.Value);

                if (status.HasValue)
                    query = query.Where(u => u.Status == status.Value);

                var units = await query
                    .OrderBy(u => u.Code)
                    .ToListAsync(ct);

                return _mapper.Map<List<UnitListItemDTO>>(units);
            }
            catch (Exception ex)
            {
                throw HelperService.MapToStatusException(ex);
            }
        }

        public async Task<UnitDetailsDTO> GetUnitById(Guid id, CancellationToken ct)
        {
            try
            {
                var unit = await _context.Units.FirstOrDefaultAsync(u => u.Id == id, ct);
                if (unit is null)
                    throw new StatusException(HttpStatusCode.NotFound, "NotFound", "Unit not found.", new { Id = "Unit does not exist." });

                return _mapper.Map<UnitDetailsDTO>(unit);
            }
            catch (Exception ex)
            {
                throw HelperService.MapToStatusException(ex);
            }
        }

        public async Task<UnitDetailsDTO> UpdateUnit(Guid id, UpdateUnitRequestDTO dto, CancellationToken ct)
        {
            try
            {
                var unit = await _context.Units.FirstOrDefaultAsync(u => u.Id == id, ct);
                if (unit is null)
                    throw new StatusException(HttpStatusCode.NotFound, "NotFound", "Unit not found.", new { Id = "Unit does not exist." });

                _mapper.Map(dto, unit);

                await _context.SaveChangesAsync(ct);

                return _mapper.Map<UnitDetailsDTO>(unit);
            }
            catch (Exception ex)
            {
                throw HelperService.MapToStatusException(ex);
            }
        }

        // Dispatch Orders
        public async Task<DispatchOrderDetailsDTO> CreateDispatchOrder(CreateDispatchOrderRequestDTO dto, CancellationToken ct)
        {
            try
            {
                var exists = await _context.DispatchOrders.AnyAsync(o => o.IncidentId == dto.IncidentId, ct);
                if (exists)
                    throw new StatusException(
                        HttpStatusCode.Conflict,
                        "Duplicate",
                        "Dispatch order for this incident already exists.",
                        new { IncidentId = "Dispatch order already exists for this incident." }
                    );

                var order = _mapper.Map<DispatchOrder>(dto);
                order.Id = Guid.NewGuid();
                order.Status = DispatchStatus.Created;
                order.CreatedAt = DateTime.UtcNow;
                order.CompletedAt = null;
                
                // Ensure Notes is initialized
                if (order.Notes == null)
                    order.Notes = new List<string>();

                _context.DispatchOrders.Add(order);
                await _context.SaveChangesAsync(ct);

                // Publish event for incident status update
                var incidentCreatorUserId = await _context.Incidents
                    .AsNoTracking()
                    .Where(i => i.Id == order.IncidentId)
                    .Select(i => i.CreatedByUserId)
                    .FirstOrDefaultAsync(ct);

                _ = Task.Run(async () => await _eventPublisher.PublishDispatchOrderCreatedAsync(order.Id, order.IncidentId, incidentCreatorUserId), ct);

                return _mapper.Map<DispatchOrderDetailsDTO>(order);
            }
            catch (Exception ex)
            {
                throw HelperService.MapToStatusException(ex);
            }
        }

        public async Task<List<DispatchOrderListItemDTO>> GetDispatchOrders(DispatchStatus? status, CancellationToken ct)
        {
            try
            {
                var query = _context.DispatchOrders.AsQueryable();

                if (status.HasValue)
                    query = query.Where(o => o.Status == status.Value);

                var orders = await query
                    .OrderByDescending(o => o.CreatedAt)
                    .ToListAsync(ct);

                return _mapper.Map<List<DispatchOrderListItemDTO>>(orders);
            }
            catch (Exception ex)
            {
                throw HelperService.MapToStatusException(ex);
            }
        }

        public async Task<DispatchOrderDetailsDTO> GetDispatchOrderById(Guid id, CancellationToken ct)
        {
            try
            {
                var order = await _context.DispatchOrders
                    .Include(o => o.Assignments)
                        .ThenInclude(a => a.Unit)
                    .FirstOrDefaultAsync(o => o.Id == id, ct);

                if (order is null)
                    throw new StatusException(
                        HttpStatusCode.NotFound,
                        "NotFound",
                        "Dispatch order not found.",
                        new { Id = "Dispatch order does not exist." }
                    );

                return _mapper.Map<DispatchOrderDetailsDTO>(order);
            }
            catch (Exception ex)
            {
                throw HelperService.MapToStatusException(ex);
            }
        }

        public async Task<DispatchOrderDetailsDTO> GetDispatchOrderByIncidentId(Guid incidentId, CancellationToken ct)
        {
            try
            {
                var order = await _context.DispatchOrders
                    .Include(o => o.Assignments)
                        .ThenInclude(a => a.Unit)
                    .FirstOrDefaultAsync(o => o.IncidentId == incidentId, ct);

                if (order is null)
                    throw new StatusException(
                        HttpStatusCode.NotFound,
                        "NotFound",
                        "Dispatch order not found for this incident.",
                        new { IncidentId = "No dispatch order exists for this incident." }
                    );

                return _mapper.Map<DispatchOrderDetailsDTO>(order);
            }
            catch (Exception ex)
            {
                throw HelperService.MapToStatusException(ex);
            }
        }

        public async Task<DispatchOrderDetailsDTO> UpdateDispatchOrderNotes(Guid id, UpdateDispatchOrderRequestDTO dto, CancellationToken ct)
        {
            try
            {
                var order = await _context.DispatchOrders
                    .Include(o => o.Assignments)
                        .ThenInclude(a => a.Unit)
                    .FirstOrDefaultAsync(o => o.Id == id, ct);

                if (order is null)
                    throw new StatusException(
                        HttpStatusCode.NotFound,
                        "NotFound",
                        "Dispatch order not found.",
                        new { Id = "Dispatch order does not exist." }
                    );

                if (order.Status is DispatchStatus.Completed or DispatchStatus.Cancelled)
                    throw new StatusException(
                        HttpStatusCode.Conflict,
                        "InvalidState",
                        "Cannot update a completed/cancelled dispatch order.",
                        new { Status = "Dispatch order is not active." }
                    );

                // Append new notes to existing notes array
                if (dto.Notes != null && dto.Notes.Any())
                {
                    if (order.Notes == null)
                        order.Notes = new List<string>();
                    
                    var updatedNotes = new List<string>(order.Notes);
                    updatedNotes.AddRange(dto.Notes);
                    order.Notes = updatedNotes;
                    
                    _context.Entry(order).Property(o => o.Notes).IsModified = true;
                }

                await _context.SaveChangesAsync(ct);

                return _mapper.Map<DispatchOrderDetailsDTO>(order);
            }
            catch (Exception ex)
            {
                throw HelperService.MapToStatusException(ex);
            }
        }
        
        // Dispatch Assignments
        public async Task<DispatchAssignmentDTO> AssignUnitToDispatch( Guid dispatchOrderId, CreateDispatchAssignmentRequestDTO dto, CancellationToken ct)
        {
            try
            {
                var order = await _context.DispatchOrders
                    .Include(o => o.Assignments)
                    .FirstOrDefaultAsync(o => o.Id == dispatchOrderId, ct);

                if (order is null)
                    throw new StatusException(HttpStatusCode.NotFound, "NotFound", "Dispatch order not found.",
                        new { DispatchOrderId = "Dispatch order does not exist." });

                if (order.Status is DispatchStatus.Completed or DispatchStatus.Cancelled)
                    throw new StatusException(HttpStatusCode.Conflict, "InvalidState", "Dispatch order is not active.",
                        new { Status = "Cannot assign units to a completed/cancelled dispatch order." });

                var unit = await _context.Units.FirstOrDefaultAsync(u => u.Id == dto.UnitId, ct);
                if (unit is null)
                    throw new StatusException(HttpStatusCode.NotFound, "NotFound", "Unit not found.",
                        new { UnitId = "Unit does not exist." });

                if (unit.Status != UnitStatus.Available)
                    throw new StatusException(HttpStatusCode.Conflict, "UnitUnavailable", "Unit is not available.",
                        new { UnitStatus = "Unit must be Available to be assigned." });

                var hasActiveAssignment = await _context.DispatchAssignments.AnyAsync(a =>
                    a.UnitId == dto.UnitId &&
                    a.Status != AssignmentStatus.Completed &&
                    a.Status != AssignmentStatus.Cancelled &&
                    a.Status != AssignmentStatus.Replaced, ct);

                if (hasActiveAssignment)
                    throw new StatusException(HttpStatusCode.Conflict, "UnitBusy", "Unit already has an active assignment.",
                        new { UnitId = "Unit is already assigned to another dispatch." });

                var alreadyAssignedToThisOrder = await _context.DispatchAssignments.AnyAsync(a =>
                    a.DispatchOrderId == dispatchOrderId && a.UnitId == dto.UnitId &&
                    a.Status != AssignmentStatus.Cancelled && a.Status != AssignmentStatus.Replaced, ct);

                if (alreadyAssignedToThisOrder)
                    throw new StatusException(HttpStatusCode.Conflict, "Duplicate", "Unit already assigned to this dispatch order.",
                        new { UnitId = "Unit is already assigned to this dispatch order." });

                var assignment = new DispatchAssignment
                {
                    Id = Guid.NewGuid(),
                    DispatchOrderId = dispatchOrderId,
                    UnitId = dto.UnitId,
                    AssignedAt = DateTime.UtcNow,
                    Status = AssignmentStatus.Assigned
                };

                _context.DispatchAssignments.Add(assignment);

                unit.Status = UnitStatus.Assigned;

                if (order.Status == DispatchStatus.Created)
                    order.Status = DispatchStatus.InProgress;

                await _context.SaveChangesAsync(ct);

                // Publish event for incident status update
                var incidentCreatorUserId = await _context.Incidents
                    .AsNoTracking()
                    .Where(i => i.Id == order.IncidentId)
                    .Select(i => i.CreatedByUserId)
                    .FirstOrDefaultAsync(ct);

                _ = Task.Run(async () => await _eventPublisher.PublishDispatchAssignmentCreatedAsync(assignment.Id, dispatchOrderId, order.IncidentId, incidentCreatorUserId), ct);

                var assignmentWithUnit = await _context.DispatchAssignments
                    .Include(a => a.Unit)
                    .FirstOrDefaultAsync(a => a.Id == assignment.Id, ct);

                return _mapper.Map<DispatchAssignmentDTO>(assignmentWithUnit);
            }
            catch (Exception ex)
            {
                throw HelperService.MapToStatusException(ex);
            }
        }

        public async Task<DispatchAssignmentDTO> UpdateDispatchAssignmentStatus(Guid assignmentId, UpdateDispatchAssignmentStatusRequestDTO dto, CancellationToken ct)
        {
            try
            {
                var assignment = await _context.DispatchAssignments
                    .Include(a => a.Unit)
                    .Include(a => a.DispatchOrder)
                        .ThenInclude(o => o.Assignments)
                    .FirstOrDefaultAsync(a => a.Id == assignmentId, ct);

                if (assignment is null)
                    throw new StatusException(HttpStatusCode.NotFound, "NotFound", "Dispatch assignment not found.",
                        new { AssignmentId = "Assignment does not exist." });

                if (assignment.DispatchOrder.Status is DispatchStatus.Completed or DispatchStatus.Cancelled)
                    throw new StatusException(HttpStatusCode.Conflict, "InvalidState", "Dispatch order is not active.",
                        new { Status = "Cannot update assignment when dispatch is completed/cancelled." });

                ValidateAssignmentTransition(assignment.Status, dto.Status);

                assignment.Status = dto.Status;

                assignment.Unit.Status = dto.Status switch
                {
                    AssignmentStatus.Assigned => UnitStatus.Assigned,
                    AssignmentStatus.EnRoute => UnitStatus.EnRoute,
                    AssignmentStatus.OnSite => UnitStatus.OnSite,

                    AssignmentStatus.Completed or AssignmentStatus.Cancelled or AssignmentStatus.Replaced => UnitStatus.Available,
                    _ => assignment.Unit.Status
                };

                var allDone = assignment.DispatchOrder.Assignments.Any() &&
                              assignment.DispatchOrder.Assignments.All(a =>
                                  a.Status is AssignmentStatus.Completed or AssignmentStatus.Cancelled or AssignmentStatus.Replaced);

                if (allDone && assignment.DispatchOrder.Status != DispatchStatus.Cancelled)
                {
                    assignment.DispatchOrder.Status = DispatchStatus.Completed;
                    assignment.DispatchOrder.CompletedAt = DateTime.UtcNow;
                    
                    await _context.SaveChangesAsync(ct);

                    // Notify user when assignment is completed (AssignmentStatus = 4)
                    if (dto.Status == AssignmentStatus.Completed)
                    {
                        var incidentCreatorUserId = await _context.Incidents
                            .AsNoTracking()
                            .Where(i => i.Id == assignment.DispatchOrder.IncidentId)
                            .Select(i => i.CreatedByUserId)
                            .FirstOrDefaultAsync(ct);

                        _ = Task.Run(async () => await _eventPublisher.PublishDispatchAssignmentCompletedAsync(
                            assignment.Id,
                            assignment.DispatchOrder.Id,
                            assignment.DispatchOrder.IncidentId,
                            incidentCreatorUserId), ct);
                    }
                    
                    // Publish event for incident status update
                    _ = Task.Run(async () => await _eventPublisher.PublishDispatchOrderCompletedAsync(
                        assignment.DispatchOrder.Id, 
                        assignment.DispatchOrder.IncidentId,
                        (await _context.Incidents.AsNoTracking()
                            .Where(i => i.Id == assignment.DispatchOrder.IncidentId)
                            .Select(i => i.CreatedByUserId)
                            .FirstOrDefaultAsync(ct))
                        ), ct);
                }
                else
                {
                    await _context.SaveChangesAsync(ct);

                    // Notify user when assignment is completed (AssignmentStatus = 4)
                    if (dto.Status == AssignmentStatus.Completed)
                    {
                        var incidentCreatorUserId = await _context.Incidents
                            .AsNoTracking()
                            .Where(i => i.Id == assignment.DispatchOrder.IncidentId)
                            .Select(i => i.CreatedByUserId)
                            .FirstOrDefaultAsync(ct);

                        _ = Task.Run(async () => await _eventPublisher.PublishDispatchAssignmentCompletedAsync(
                            assignment.Id,
                            assignment.DispatchOrder.Id,
                            assignment.DispatchOrder.IncidentId,
                            incidentCreatorUserId), ct);
                    }
                }

                return _mapper.Map<DispatchAssignmentDTO>(assignment);
            }
            catch (Exception ex)
            {
                throw HelperService.MapToStatusException(ex);
            }
        }


        // Helpers (private)
        private static void ValidateAssignmentTransition(AssignmentStatus current, AssignmentStatus next)
        {
            var ok = current switch
            {
                AssignmentStatus.Assigned => next is AssignmentStatus.EnRoute or AssignmentStatus.Cancelled or AssignmentStatus.Replaced,
                AssignmentStatus.EnRoute => next is AssignmentStatus.OnSite or AssignmentStatus.Cancelled,
                AssignmentStatus.OnSite => next is AssignmentStatus.Completed or AssignmentStatus.Cancelled,

                AssignmentStatus.Completed => false,
                AssignmentStatus.Cancelled => false,
                AssignmentStatus.Replaced => false,
                _ => false
            };

            if (!ok)
                throw new StatusException(HttpStatusCode.Conflict, "InvalidTransition",
                    $"Invalid assignment transition: {current} -> {next}",
                    new { Transition = "Assignment status transition is not allowed." });
        }

    }
}
