using InventoryX.Domain.Models.Common;

namespace InventoryX.Domain.Models.Auditing;

public sealed class NotificationPreference : BaseModel
{
    public required string UserId { get; set; }
    public NotificationType Type { get; set; }
    public NotificationChannel Channel { get; set; }
    public bool IsEnabled { get; set; }
    public decimal? Threshold { get; set; }
}

/// <summary>Per-user read state, required because tenant-wide notifications have no target user.</summary>
public sealed class NotificationReadState : BaseModel
{
    public Guid NotificationId { get; set; }
    public Notification? Notification { get; set; }
    public required string UserId { get; set; }
    public DateTime ReadAt { get; set; }
}
