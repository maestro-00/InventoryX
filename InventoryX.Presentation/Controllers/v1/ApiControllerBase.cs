using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

namespace InventoryX.Presentation.Controllers.v1
{
    /// <summary>
    /// Base class for all versioned API controllers: routes under /api/v1,
    /// exposes tenant/user claim accessors and rowversion ETag helpers.
    /// </summary>
    [ApiController]
    [Route("api/v1/[controller]")]
    public abstract class ApiControllerBase : ControllerBase
    {
        protected Guid? TenantId =>
            Guid.TryParse(User.FindFirstValue("tenant_id"), out var tenantId) ? tenantId : null;

        protected string? UserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

        protected string? UserRole => User.FindFirstValue(ClaimTypes.Role);

        /// <summary>Sets a weak ETag response header derived from a rowversion token.</summary>
        protected void SetETag(byte[]? rowVersion)
        {
            if (rowVersion is null || rowVersion.Length == 0) return;
            Response.Headers.ETag = ToETag(rowVersion);
        }

        /// <summary>Checks the If-Match request header against the entity's current rowversion.</summary>
        protected bool IfMatchSatisfied(byte[]? rowVersion)
        {
            var ifMatch = Request.Headers[HeaderNames.IfMatch];
            if (ifMatch.Count == 0) return true;
            if (rowVersion is null || rowVersion.Length == 0) return false;
            var current = ToETag(rowVersion);
            return ifMatch.Any(v => v == "*" || v == current);
        }

        private static string ToETag(byte[] rowVersion) =>
            $"W/\"{Convert.ToBase64String(rowVersion)}\"";
    }
}
