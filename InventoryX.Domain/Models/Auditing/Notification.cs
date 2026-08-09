using InventoryX.Domain.Models.Common;

namespace InventoryX.Domain.Models.Auditing
{
    public enum NotificationType
    {
        LowStock, OutOfStock, ExpiringStock, PoReceived, PoOverdue, TransferAwaitingReceipt,
        LargeDiscount, LargeRefund, TillVariance, UnusualVoids, NegativeStock,
        BillingFailure, StockConflict, DailyDigest, WeeklyDigest,
    }

    public enum NotificationChannel { InApp, Email, Push, Sms }

    /// <summary>
    /// A raised alert instance. Repeats for the same unresolved issue merge by
    /// ConsolidationKey, bumping OccurrenceCount instead of duplicating rows
    /// (FR-052/53).
    /// </summary>
    public class Notification : BaseModel
    {
        public NotificationType Type { get; set; }
        public NotificationChannel Channel { get; set; } = NotificationChannel.InApp;
        /// <summary>Stable key identifying the underlying issue, e.g. "low-stock:{productId}:{locationId}".</summary>
        public required string ConsolidationKey { get; set; }
        public required string Title { get; set; }
        public string? Message { get; set; }
        /// <summary>Target user; null = all users with the matching preference/role.</summary>
        public string? UserId { get; set; }
        public bool IsRead { get; set; }
        public int OccurrenceCount { get; set; } = 1;
        public DateTime LastRaisedAt { get; set; }
        public DateTime? ResolvedAt { get; set; }
    }
}
