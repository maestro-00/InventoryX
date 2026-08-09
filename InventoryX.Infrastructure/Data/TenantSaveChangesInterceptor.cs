using InventoryX.Application.Services.IServices;
using InventoryX.Domain.Models.Auditing;
using InventoryX.Domain.Models.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace InventoryX.Infrastructure.Data
{
    /// <summary>
    /// Stamps TenantId and audit fields on save, rejects cross-tenant writes
    /// (constitution G6 release-blocking rule) and enforces append-only
    /// semantics for the audit log.
    /// </summary>
    public class TenantSaveChangesInterceptor(ITenantContext tenantContext) : SaveChangesInterceptor
    {
        public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
        {
            ApplyRules(eventData.Context);
            return base.SavingChanges(eventData, result);
        }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            ApplyRules(eventData.Context);
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        private void ApplyRules(DbContext? context)
        {
            if (context is null) return;
            var now = DateTime.UtcNow;
            var currentTenant = tenantContext.TenantId;
            var currentUser = tenantContext.UserId;

            foreach (var entry in context.ChangeTracker.Entries())
            {
                if (entry.Entity is AuditLogEntry && entry.State is EntityState.Modified or EntityState.Deleted)
                    throw new InvalidOperationException("AuditLogEntry is append-only; entries cannot be modified or deleted.");

                if (entry.Entity is GlobalModel global)
                {
                    switch (entry.State)
                    {
                        case EntityState.Added:
                            global.CreatedAt = global.CreatedAt == default ? now : global.CreatedAt;
                            global.CreatedBy ??= currentUser;
                            break;
                        case EntityState.Modified:
                            global.UpdatedAt = now;
                            global.UpdatedBy = currentUser;
                            break;
                    }
                }

                if (entry.Entity is BaseModel tenantOwned)
                {
                    switch (entry.State)
                    {
                        case EntityState.Added when tenantOwned.TenantId == Guid.Empty:
                            tenantOwned.TenantId = currentTenant
                                ?? throw new InvalidOperationException(
                                    "Cannot save a tenant-owned entity without a tenant context.");
                            break;
                        case EntityState.Added:
                        case EntityState.Modified:
                        case EntityState.Deleted:
                            if (currentTenant is not null && tenantOwned.TenantId != currentTenant)
                                throw new InvalidOperationException(
                                    $"Rejected cross-tenant write: entity {entry.Metadata.ClrType.Name} belongs to another tenant.");
                            break;
                    }
                }
            }
        }
    }
}
