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

        /// <summary>Sets a weak ETag from an opaque string token (e.g. Identity ConcurrencyStamp).</summary>
        protected void SetETag(string? opaqueToken)
        {
            if (string.IsNullOrWhiteSpace(opaqueToken)) return;
            Response.Headers.ETag = $"W/\"{opaqueToken}\"";
        }

        /// <summary>
        /// Parses If-Match into the raw rowversion bytes when present and well-formed.
        /// Returns null when the header is absent (caller treats as unconditional).
        /// </summary>
        protected byte[]? ParseIfMatchRowVersion()
        {
            var ifMatch = Request.Headers[HeaderNames.IfMatch];
            if (ifMatch.Count == 0) return null;
            foreach (var value in ifMatch)
            {
                if (string.IsNullOrWhiteSpace(value) || value == "*") continue;
                var token = value.Trim();
                if (token.StartsWith("W/", StringComparison.OrdinalIgnoreCase))
                    token = token[2..].Trim();
                if (token.Length >= 2 && token[0] == '"' && token[^1] == '"')
                    token = token[1..^1];
                try
                {
                    return Convert.FromBase64String(token);
                }
                catch (FormatException)
                {
                    // fall through
                }
            }

            return [];
        }

        /// <summary>Parses If-Match as an opaque string (ConcurrencyStamp).</summary>
        protected string? ParseIfMatchOpaque()
        {
            var ifMatch = Request.Headers[HeaderNames.IfMatch];
            if (ifMatch.Count == 0) return null;
            var value = ifMatch.FirstOrDefault();
            if (string.IsNullOrWhiteSpace(value) || value == "*") return value == "*" ? "*" : null;
            var token = value.Trim();
            if (token.StartsWith("W/", StringComparison.OrdinalIgnoreCase))
                token = token[2..].Trim();
            if (token.Length >= 2 && token[0] == '"' && token[^1] == '"')
                token = token[1..^1];
            return token;
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

        /// <summary>Checks If-Match against an opaque concurrency stamp.</summary>
        protected bool IfMatchSatisfied(string? opaqueToken)
        {
            var ifMatch = Request.Headers[HeaderNames.IfMatch];
            if (ifMatch.Count == 0) return true;
            if (string.IsNullOrWhiteSpace(opaqueToken)) return false;
            var current = $"W/\"{opaqueToken}\"";
            return ifMatch.Any(v => v == "*" || v == current || v == opaqueToken);
        }

        private static string ToETag(byte[] rowVersion) =>
            $"W/\"{Convert.ToBase64String(rowVersion)}\"";
    }
}
