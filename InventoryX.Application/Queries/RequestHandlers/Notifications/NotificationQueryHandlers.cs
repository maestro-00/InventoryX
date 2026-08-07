using AutoMapper;
using InventoryX.Application.DTOs.Common;
using InventoryX.Application.DTOs.Notifications;
using InventoryX.Application.Queries.Requests.Notifications;
using InventoryX.Application.Repository;
using InventoryX.Application.Services.IServices;
using InventoryX.Domain.Models.Auditing;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InventoryX.Application.Queries.RequestHandlers.Notifications;

public sealed class GetNotificationsQueryHandler(IAppDbContext context, ITenantContext tenantContext, IMapper mapper)
    : IRequestHandler<GetNotificationsQuery, PagedResult<NotificationDto>>
{
    public async Task<PagedResult<NotificationDto>> Handle(GetNotificationsQuery request, CancellationToken cancellationToken)
    {
        var userId = NotificationUser.Require(tenantContext);
        var disabledTypes = await context.NotificationPreferences.AsNoTracking()
            .Where(item => item.UserId == userId && item.Channel == NotificationChannel.InApp && !item.IsEnabled)
            .Select(item => item.Type).ToListAsync(cancellationToken);
        var readIds = context.NotificationReadStates.AsNoTracking()
            .Where(item => item.UserId == userId).Select(item => item.NotificationId);
        var query = context.Notifications.AsNoTracking()
            .Where(item => item.Channel == NotificationChannel.InApp &&
                           (item.UserId == null || item.UserId == userId) &&
                           !disabledTypes.Contains(item.Type));
        if (request.UnreadOnly) query = query.Where(item => !item.IsRead && !readIds.Contains(item.Id));

        var total = await query.LongCountAsync(cancellationToken);
        var notifications = await query.OrderByDescending(item => item.LastRaisedAt).ThenByDescending(item => item.Id)
            .Skip(request.Skip).Take(request.PageSize).ToListAsync(cancellationToken);
        var pageIds = notifications.Select(item => item.Id).ToList();
        var pageReadIds = (await readIds.Where(item => pageIds.Contains(item)).ToListAsync(cancellationToken)).ToHashSet();
        var items = notifications.Select(item =>
        {
            var dto = mapper.Map<NotificationDto>(item);
            dto.IsRead = item.IsRead || pageReadIds.Contains(item.Id);
            return dto;
        }).ToList();
        return PagedResult<NotificationDto>.Create(items, request.Page, request.PageSize, total);
    }
}

public sealed class GetNotificationPreferencesQueryHandler(IAppDbContext context, ITenantContext tenantContext, IMapper mapper)
    : IRequestHandler<GetNotificationPreferencesQuery, IReadOnlyList<NotificationPreferenceDto>>
{
    public async Task<IReadOnlyList<NotificationPreferenceDto>> Handle(
        GetNotificationPreferencesQuery request, CancellationToken cancellationToken)
    {
        var userId = NotificationUser.Require(tenantContext);
        var preferences = await context.NotificationPreferences.AsNoTracking()
            .Where(item => item.UserId == userId).ToListAsync(cancellationToken);
        return NotificationPreferenceMatrix.Build(preferences, mapper);
    }
}

internal static class NotificationPreferenceMatrix
{
    public static IReadOnlyList<NotificationPreferenceDto> Build(IEnumerable<NotificationPreference> preferences, IMapper mapper)
    {
        var overrides = preferences.ToDictionary(item => (item.Type, item.Channel));
        return (from type in Enum.GetValues<NotificationType>()
                from channel in Enum.GetValues<NotificationChannel>()
                let key = (type, channel)
                select overrides.TryGetValue(key, out var preference)
                    ? mapper.Map<NotificationPreferenceDto>(preference)
                    : new NotificationPreferenceDto
                    {
                        Type = type, Channel = channel, IsEnabled = channel == NotificationChannel.InApp,
                    }).ToList();
    }
}

internal static class NotificationUser
{
    public static string Require(ITenantContext tenantContext) =>
        tenantContext.UserId ?? throw new UnauthorizedAccessException("A user identity is required for notifications.");
}
