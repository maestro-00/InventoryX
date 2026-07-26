using System.Reflection;
using InventoryX.Application.Repository;
using InventoryX.Application.Services.IServices;
using InventoryX.Domain.Models;
using InventoryX.Domain.Models.Auditing;
using InventoryX.Domain.Models.Catalog;
using InventoryX.Domain.Models.Common;
using InventoryX.Domain.Models.Tenancy;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace InventoryX.Infrastructure.Data
{
    public class AppDbContext(DbContextOptions<AppDbContext> options, ITenantContext? tenantContext = null)
        : IdentityDbContext<User>(options), IAppDbContext
    {
        private readonly ITenantContext? _tenantContext = tenantContext;

        /// <summary>Tenant the global query filters scope every tenant-owned set to.</summary>
        public Guid? CurrentTenantId => _tenantContext?.TenantId;

        public DbSet<Tenant> Tenants => Set<Tenant>();
        public DbSet<PlanDefinition> PlanDefinitions => Set<PlanDefinition>();
        public DbSet<Subscription> Subscriptions => Set<Subscription>();
        public DbSet<UsageCounter> UsageCounters => Set<UsageCounter>();
        public DbSet<Role> AppRoles => Set<Role>();
        public DbSet<AuditLogEntry> AuditLogEntries => Set<AuditLogEntry>();
        public DbSet<Notification> Notifications => Set<Notification>();
        public DbSet<TaxTreatment> TaxTreatments => Set<TaxTreatment>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Tenant>().HasIndex(t => t.Name);

            builder.Entity<PlanDefinition>().HasIndex(p => new { p.Tier, p.IsActive });

            builder.Entity<Subscription>()
                .HasOne(s => s.Plan).WithMany().HasForeignKey(s => s.PlanDefinitionId).OnDelete(DeleteBehavior.Restrict);
            builder.Entity<Subscription>().HasIndex(s => new { s.TenantId, s.Status });

            builder.Entity<UsageCounter>()
                .HasIndex(c => new { c.TenantId, c.Metric, c.PeriodKey }).IsUnique();

            builder.Entity<Role>().HasIndex(r => r.Name).IsUnique();

            builder.Entity<AuditLogEntry>().HasIndex(a => new { a.TenantId, a.OccurredAt });

            builder.Entity<Notification>()
                .HasIndex(n => new { n.TenantId, n.ConsolidationKey, n.Channel });

            builder.Entity<TaxTreatment>().HasIndex(t => t.Code).IsUnique();

            builder.Entity<User>().HasIndex(u => u.TenantId);

            ApplyTenantQueryFilters(builder);
        }

        /// <summary>
        /// Applies the release-blocking tenant isolation filter (constitution G6)
        /// to every root entity deriving from BaseModel.
        /// </summary>
        private void ApplyTenantQueryFilters(ModelBuilder builder)
        {
            var applyMethod = typeof(AppDbContext)
                .GetMethod(nameof(ApplyTenantFilter), BindingFlags.NonPublic | BindingFlags.Instance)!;

            foreach (var entityType in builder.Model.GetEntityTypes())
            {
                if (entityType.BaseType is null && typeof(BaseModel).IsAssignableFrom(entityType.ClrType))
                {
                    applyMethod.MakeGenericMethod(entityType.ClrType).Invoke(this, [builder]);
                }
            }
        }

        private void ApplyTenantFilter<TEntity>(ModelBuilder builder) where TEntity : BaseModel
        {
            builder.Entity<TEntity>()
                .HasQueryFilter(e => CurrentTenantId != null && e.TenantId == CurrentTenantId);
        }
    }
}
