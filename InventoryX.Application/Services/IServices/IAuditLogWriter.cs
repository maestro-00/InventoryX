namespace InventoryX.Application.Services.IServices
{
    /// <summary>Persists append-only audit entries; implemented in Infrastructure.</summary>
    public interface IAuditLogWriter
    {
        Task WriteAsync(
            string action,
            string entityType,
            string entityId,
            object? before = null,
            object? after = null,
            CancellationToken cancellationToken = default);
    }
}
