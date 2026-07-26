using InventoryX.Application.Services.IServices;

namespace InventoryX.Infrastructure.Services
{
    /// <summary>
    /// Scoped per-request tenant context; populated by the tenant-resolution
    /// middleware from JWT claims (T016) and read by the DbContext filters,
    /// interceptor and behaviors.
    /// </summary>
    public class HttpTenantContext : ITenantContext
    {
        public Guid? TenantId { get; set; }
        public string? UserId { get; set; }
        public string? Role { get; set; }
        public string? LocationScope { get; set; }
    }
}
