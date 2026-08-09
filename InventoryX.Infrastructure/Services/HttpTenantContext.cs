using System.Security.Claims;
using InventoryX.Application.Services.IServices;
using Microsoft.AspNetCore.Http;

namespace InventoryX.Infrastructure.Services
{
    /// <summary>
    /// Scoped per-request tenant context; populated by the tenant-resolution
    /// middleware from JWT claims (T016) and read by the DbContext filters,
    /// interceptor and behaviors.
    /// </summary>
    public class HttpTenantContext(IHttpContextAccessor httpContextAccessor) : ITenantContext
    {
        private Guid? _tenantId;
        private string? _userId;
        private string? _role;
        private string? _locationScope;

        public Guid? TenantId
        {
            get
            {
                if (_tenantId is not null) return _tenantId;
                return Guid.TryParse(httpContextAccessor.HttpContext?.User.FindFirstValue("tenant_id"), out var tenantId)
                    ? tenantId
                    : null;
            }
            set => _tenantId = value;
        }

        public string? UserId
        {
            get => _userId ?? httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
            set => _userId = value;
        }

        public string? Role
        {
            get => _role ?? httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Role);
            set => _role = value;
        }

        public string? LocationScope
        {
            get => _locationScope ?? httpContextAccessor.HttpContext?.User.FindFirstValue("location_scope");
            set => _locationScope = value;
        }
    }
}
