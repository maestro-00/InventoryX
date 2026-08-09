using InventoryX.Application.Behaviors;
using InventoryX.Application.DTOs.Notifications;
using InventoryX.Domain.Models.Auditing;
using MediatR;

namespace InventoryX.Application.Commands.Requests.Notifications;

public sealed record NotificationPreferenceInput(
    NotificationType Type,
    NotificationChannel Channel,
    bool IsEnabled,
    decimal? Threshold);

public sealed class UpdateNotificationPreferencesCommand
    : IRequest<IReadOnlyList<NotificationPreferenceDto>>, IReadOnlyWriteExemptCommand
{
    public List<NotificationPreferenceInput> Preferences { get; init; } = [];
}

public sealed record MarkNotificationReadCommand(Guid Id) : IRequest<bool>, IReadOnlyWriteExemptCommand;
public sealed record MarkAllNotificationsReadCommand : IRequest<int>, IReadOnlyWriteExemptCommand;
