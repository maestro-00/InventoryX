using AutoMapper;
using InventoryX.Application.Commands.Requests.Notifications;
using InventoryX.Application.DTOs.Notifications;
using InventoryX.Application.Exceptions;
using InventoryX.Application.Queries.RequestHandlers.Notifications;
using InventoryX.Application.Repository;
using InventoryX.Application.Services.IServices;
using InventoryX.Domain.Models.Auditing;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InventoryX.Application.Commands.RequestHandlers.Notifications;

public sealed class UpdateNotificationPreferencesCommandHandler(
    IAppDbContext context, ITenantContext tenantContext, IMapper mapper)
    : IRequestHandler<UpdateNotificationPreferencesCommand, IReadOnlyList<NotificationPreferenceDto>>
{
    public async Task<IReadOnlyList<NotificationPreferenceDto>> Handle(
        UpdateNotificationPreferencesCommand request, CancellationToken cancellationToken)
    {
        var userId = NotificationUser.Require(tenantContext);
        var stored = await context.NotificationPreferences.Where(item => item.UserId == userId)
            .ToListAsync(cancellationToken);
        var byKey = stored.ToDictionary(item => (item.Type, item.Channel));
        foreach (var input in request.Preferences)
        {
            if (!byKey.TryGetValue((input.Type, input.Channel), out var preference))
            {
                preference = new NotificationPreference
                {
                    UserId = userId, Type = input.Type, Channel = input.Channel,
                };
                context.NotificationPreferences.Add(preference);
                stored.Add(preference);
                byKey.Add((input.Type, input.Channel), preference);
            }
            preference.IsEnabled = input.IsEnabled;
            preference.Threshold = input.Threshold;
        }
        await context.SaveChangesAsync(cancellationToken);
        return NotificationPreferenceMatrix.Build(stored, mapper);
    }
}

public sealed class MarkNotificationReadCommandHandler(IAppDbContext context, ITenantContext tenantContext)
    : IRequestHandler<MarkNotificationReadCommand, bool>
{
    public async Task<bool> Handle(MarkNotificationReadCommand request, CancellationToken cancellationToken)
    {
        var userId = NotificationUser.Require(tenantContext);
        var notification = await context.Notifications.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == request.Id &&
                                          item.Channel == NotificationChannel.InApp &&
                                          (item.UserId == null || item.UserId == userId), cancellationToken)
            ?? throw new NotFoundException("Notification not found.");
        if (!await context.NotificationReadStates.AnyAsync(
                item => item.NotificationId == notification.Id && item.UserId == userId, cancellationToken))
        {
            context.NotificationReadStates.Add(new NotificationReadState
            {
                NotificationId = notification.Id, UserId = userId, ReadAt = DateTime.UtcNow,
            });
            await context.SaveChangesAsync(cancellationToken);
        }
        return true;
    }
}

public sealed class MarkAllNotificationsReadCommandHandler(IAppDbContext context, ITenantContext tenantContext)
    : IRequestHandler<MarkAllNotificationsReadCommand, int>
{
    public async Task<int> Handle(MarkAllNotificationsReadCommand request, CancellationToken cancellationToken)
    {
        var userId = NotificationUser.Require(tenantContext);
        var disabledTypes = await context.NotificationPreferences.AsNoTracking()
            .Where(item => item.UserId == userId && item.Channel == NotificationChannel.InApp && !item.IsEnabled)
            .Select(item => item.Type).ToListAsync(cancellationToken);
        var readIds = context.NotificationReadStates.AsNoTracking()
            .Where(item => item.UserId == userId).Select(item => item.NotificationId);
        var unreadIds = await context.Notifications.AsNoTracking()
            .Where(item => item.Channel == NotificationChannel.InApp && !item.IsRead &&
                           (item.UserId == null || item.UserId == userId) &&
                           !disabledTypes.Contains(item.Type) && !readIds.Contains(item.Id))
            .Select(item => item.Id).ToListAsync(cancellationToken);
        var now = DateTime.UtcNow;
        context.NotificationReadStates.AddRange(unreadIds.Select(notificationId => new NotificationReadState
        {
            NotificationId = notificationId, UserId = userId, ReadAt = now,
        }));
        if (unreadIds.Count > 0) await context.SaveChangesAsync(cancellationToken);
        return unreadIds.Count;
    }
}
