namespace InventoryX.Application.Services.IServices
{
    /// <summary>
    /// Per-request tenant/user identity resolved from JWT claims by the
    /// tenant-resolution middleware and consumed by the EF global query filter,
    /// SaveChanges interceptor and pipeline behaviors.
    /// </summary>
    public interface ITenantContext
    {
        Guid? TenantId { get; set; }
        string? UserId { get; set; }
        string? Role { get; set; }
        /// <summary>Comma-separated location ids the caller may operate on; "*" = all.</summary>
        string? LocationScope { get; set; }
    }
}
