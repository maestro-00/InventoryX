using InventoryX.Application.DTOs.Common;
using InventoryX.Application.DTOs.Notifications;
using MediatR;

namespace InventoryX.Application.Queries.Requests.Notifications;

public sealed record GetNotificationsQuery : PageRequest, IRequest<PagedResult<NotificationDto>>
{
    public bool UnreadOnly { get; init; }
}

public sealed record GetNotificationPreferencesQuery : IRequest<IReadOnlyList<NotificationPreferenceDto>>;
