using AutoMapper;
using InventoryX.Application.DTOs.Notifications;
using InventoryX.Domain.Models.Auditing;

namespace InventoryX.Application.Extensions;

public sealed class NotificationMappingProfile : Profile
{
    public NotificationMappingProfile()
    {
        CreateMap<Notification, NotificationDto>()
            .ForMember(destination => destination.Occurrences,
                options => options.MapFrom(source => source.OccurrenceCount));
        CreateMap<NotificationPreference, NotificationPreferenceDto>();
    }
}
