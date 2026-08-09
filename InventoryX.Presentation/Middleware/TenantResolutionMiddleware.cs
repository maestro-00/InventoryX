using System.Security.Claims;
using InventoryX.Application.Services.IServices;

namespace InventoryX.Presentation.Middleware
{
    /// <summary>
    /// Populates the scoped ITenantContext from the authenticated principal's
    /// tenant_id / role / location_scope JWT claims (T016). Authenticated
    /// requests without a resolvable tenant claim are rejected with 401;
    /// anonymous requests pass through for endpoint-level authorization to
    /// decide.
    /// </summary>
    public class TenantResolutionMiddleware(RequestDelegate next, ILogger<TenantResolutionMiddleware> logger)
    {
        public async Task InvokeAsync(HttpContext context, ITenantContext tenantContext)
        {
            var identity = context.User.Identity;
            if (identity?.IsAuthenticated == true)
            {
                var tenantClaim = context.User.FindFirstValue("tenant_id");
                if (!Guid.TryParse(tenantClaim, out var tenantId))
                {
                    logger.LogWarning("Authenticated request without a valid tenant_id claim rejected for {Path}", context.Request.Path);
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    context.Response.ContentType = "application/problem+json";
                    await context.Response.WriteAsync(
                        """{"type":"https://inventoryx.app/problems/no-tenant","title":"The credential carries no tenant.","status":401}""");
                    return;
                }

                tenantContext.TenantId = tenantId;
                tenantContext.UserId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
                tenantContext.Role = context.User.FindFirstValue(ClaimTypes.Role);
                tenantContext.LocationScope = context.User.FindFirstValue("location_scope");
            }

            await next(context);
        }
    }
}
