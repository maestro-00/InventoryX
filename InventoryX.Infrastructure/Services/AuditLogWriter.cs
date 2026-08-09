using System.Text.Json;
using InventoryX.Application.Services.IServices;
using InventoryX.Domain.Models.Auditing;
using InventoryX.Infrastructure.Data;
using Microsoft.AspNetCore.Http;

namespace InventoryX.Infrastructure.Services
{
    public class AuditLogWriter(AppDbContext context, ITenantContext tenantContext, IHttpContextAccessor httpContextAccessor)
        : IAuditLogWriter
    {
        private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

        public async Task WriteAsync(
            string action,
            string entityType,
            string entityId,
            object? before = null,
            object? after = null,
            CancellationToken cancellationToken = default)
        {
            context.AuditLogEntries.Add(new AuditLogEntry
            {
                Actor = tenantContext.UserId ?? "system",
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                BeforeJson = before is null ? null : JsonSerializer.Serialize(before, SerializerOptions),
                AfterJson = after is null ? null : JsonSerializer.Serialize(after, SerializerOptions),
                OccurredAt = DateTime.UtcNow,
                Ip = httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString(),
            });
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}
