using AutoMapper;
using DispatchService.Domain;
using DispatchService.DTOs.DispatchAssignments;
using DispatchService.DTOs.DispatchOrders;
using DispatchService.DTOs.Units;

namespace DispatchService.Helpers
{
    public class MappingProfiles : Profile
    {
        public MappingProfiles()
        {
            // Units 
            CreateMap<Unit, UnitListItemDTO>();
            CreateMap<Unit, UnitDetailsDTO>();

            CreateMap<CreateUnitRequestDTO, Unit>()
                .ForMember(d => d.Id, opt => opt.Ignore())
                .ForMember(d => d.Status, opt => opt.Ignore())
                .ForMember(d => d.DispatchAssignments, opt => opt.Ignore());

            CreateMap<UpdateUnitRequestDTO, Unit>()
                .ForMember(d => d.Id, opt => opt.Ignore())
                .ForMember(d => d.Code, opt => opt.Ignore()) 
                .ForMember(d => d.Type, opt => opt.Ignore())
                .ForMember(d => d.DispatchAssignments, opt => opt.Ignore());

            // Dispatch Orders
            CreateMap<DispatchOrder, DispatchOrderListItemDTO>()
                .ForMember(d => d.AssignmentsCount, opt => opt.MapFrom(s => s.Assignments.Count));

            CreateMap<DispatchOrder, DispatchOrderDetailsDTO>();

            CreateMap<CreateDispatchOrderRequestDTO, DispatchOrder>()
                .ForMember(d => d.Id, opt => opt.Ignore())
                .ForMember(d => d.Status, opt => opt.Ignore())
                .ForMember(d => d.CreatedAt, opt => opt.Ignore())
                .ForMember(d => d.CompletedAt, opt => opt.Ignore())
                .ForMember(d => d.Assignments, opt => opt.Ignore());

            CreateMap<UpdateDispatchOrderRequestDTO, DispatchOrder>()
                .ForMember(d => d.Id, opt => opt.Ignore())
                .ForMember(d => d.IncidentId, opt => opt.Ignore())
                .ForMember(d => d.Status, opt => opt.Ignore())
                .ForMember(d => d.CreatedAt, opt => opt.Ignore())
                .ForMember(d => d.Assignments, opt => opt.Ignore());

            // Dispatch Assignments
            CreateMap<DispatchAssignment, DispatchAssignmentDTO>()
                .ForMember(d => d.UnitCode, opt => opt.MapFrom(s => s.Unit.Code));

            CreateMap<CreateDispatchAssignmentRequestDTO, DispatchAssignment>()
                .ForMember(d => d.Id, opt => opt.Ignore())
                .ForMember(d => d.DispatchOrderId, opt => opt.Ignore()) 
                .ForMember(d => d.AssignedAt, opt => opt.Ignore())
                .ForMember(d => d.Status, opt => opt.Ignore())
                .ForMember(d => d.Unit, opt => opt.Ignore())
                .ForMember(d => d.DispatchOrder, opt => opt.Ignore());

            CreateMap<UpdateDispatchAssignmentStatusRequestDTO, DispatchAssignment>()
                .ForMember(d => d.Id, opt => opt.Ignore())
                .ForMember(d => d.DispatchOrderId, opt => opt.Ignore())
                .ForMember(d => d.UnitId, opt => opt.Ignore())
                .ForMember(d => d.AssignedAt, opt => opt.Ignore())
                .ForMember(d => d.Unit, opt => opt.Ignore())
                .ForMember(d => d.DispatchOrder, opt => opt.Ignore());
        }
    }
}
