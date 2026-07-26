using InventoryX.Domain.Models.Auditing;

namespace InventoryX.Application.Services.IServices
{
    /// <summary>
    /// Raises alerts/notifications with consolidation semantics: a repeat raise
    /// for the same unresolved ConsolidationKey bumps its occurrence count
    /// instead of creating a duplicate (FR-052/53).
    /// </summary>
    public interface INotificationService
    {
        Task RaiseAsync(
            NotificationType type,
            string consolidationKey,
            string title,
            string? message = null,
            string? userId = null,
            NotificationChannel channel = NotificationChannel.InApp,
            CancellationToken cancellationToken = default);

        /// <summary>Marks all unresolved notifications with the key as resolved.</summary>
        Task ResolveAsync(string consolidationKey, CancellationToken cancellationToken = default);
    }
}
