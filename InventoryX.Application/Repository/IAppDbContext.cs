using InventoryX.Domain.Models.Auditing;
using InventoryX.Domain.Models.Catalog;
using InventoryX.Domain.Models.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace InventoryX.Application.Repository
{
    /// <summary>
    /// Application-layer view of the persistence context. Implemented by
    /// Infrastructure's AppDbContext; tenant scoping is enforced there via
    /// global query filters and the SaveChanges interceptor.
    /// </summary>
    public interface IAppDbContext
    {
        DbSet<Tenant> Tenants { get; }
        DbSet<PlanDefinition> PlanDefinitions { get; }
        DbSet<Subscription> Subscriptions { get; }
        DbSet<UsageCounter> UsageCounters { get; }
        DbSet<Role> AppRoles { get; }
        DbSet<AuditLogEntry> AuditLogEntries { get; }
        DbSet<Notification> Notifications { get; }
        DbSet<TaxTreatment> TaxTreatments { get; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
