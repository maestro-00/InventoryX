using InventoryX.Domain.Models.Auditing;

namespace InventoryX.Application.DTOs.Notifications;

public sealed class NotificationDto
{
    public Guid Id { get; init; }
    public NotificationType Type { get; init; }
    public NotificationChannel Channel { get; init; }
    public required string Title { get; init; }
    public string? Message { get; init; }
    public int Occurrences { get; init; }
    public bool IsRead { get; set; }
    public DateTime LastRaisedAt { get; init; }
    public DateTime? ResolvedAt { get; init; }
}

public sealed class NotificationPreferenceDto
{
    public NotificationType Type { get; init; }
    public NotificationChannel Channel { get; init; }
    public bool IsEnabled { get; init; }
    public decimal? Threshold { get; init; }
}
