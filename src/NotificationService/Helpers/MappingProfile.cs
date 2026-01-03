using AutoMapper;
using NotificationService.Application.Notification;
using NotificationService.Application.Notifications;
using NotificationService.Domain;
using NotificationService.DTOs.NotificationService.DTOs;

namespace NotificationService.Helpers
{
    public class NotificationMappingProfile : Profile
    {
        public NotificationMappingProfile()
        {
            CreateMap<Notification, NotificationDTO>();
            CreateMap<Create.Command, NotificationService.Domain.Notification>();
        }
    }
}
