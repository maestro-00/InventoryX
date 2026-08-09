using System.Diagnostics;
using System.Security.Claims;
using Serilog;

namespace InventoryX.Presentation.Configuration;

public sealed record RequestLogIdentity(string TenantId, string UserId, string TraceId);

public static class RequestLogEnricher
{
    private const string Anonymous = "anonymous";

    public static RequestLogIdentity Read(HttpContext context)
    {
        var tenantId = context.User.FindFirstValue("tenant_id") ?? Anonymous;
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? Anonymous;
        var traceId = Activity.Current?.TraceId.ToString();

        return new RequestLogIdentity(
            tenantId,
            userId,
            string.IsNullOrWhiteSpace(traceId) ? context.TraceIdentifier : traceId);
    }

    public static void Enrich(IDiagnosticContext diagnosticContext, HttpContext context)
    {
        var identity = Read(context);
        diagnosticContext.Set(nameof(identity.TenantId), identity.TenantId);
        diagnosticContext.Set(nameof(identity.UserId), identity.UserId);
        diagnosticContext.Set(nameof(identity.TraceId), identity.TraceId);
    }
}
