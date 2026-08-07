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

/// <summary>Idempotency record for one user's completed daily or weekly digest period.</summary>
public sealed class NotificationDigestDelivery : BaseModel
{
    public required string UserId { get; set; }
    public NotificationType DigestType { get; set; }
    public required string PeriodKey { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public DateTime ProcessedAt { get; set; }
    public int NotificationCount { get; set; }
    public int OccurrenceCount { get; set; }
}
