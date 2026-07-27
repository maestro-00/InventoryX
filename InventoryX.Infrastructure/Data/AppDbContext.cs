using System.Reflection;
using InventoryX.Application.Repository;
using InventoryX.Application.Services.IServices;
using InventoryX.Domain.Models;
using InventoryX.Domain.Models.Auditing;
using InventoryX.Domain.Models.Catalog;
using InventoryX.Domain.Models.Common;
using InventoryX.Domain.Models.Inventory;
using InventoryX.Domain.Models.Selling;
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

        public DbSet<Location> Locations => Set<Location>();
        public DbSet<Category> Categories => Set<Category>();
        public DbSet<Product> Products => Set<Product>();
        public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();
        public DbSet<StockLevel> StockLevels => Set<StockLevel>();
        public DbSet<StockMovement> StockMovements => Set<StockMovement>();
        public DbSet<Register> Registers => Set<Register>();
        public DbSet<Shift> Shifts => Set<Shift>();
        public DbSet<Sale> Sales => Set<Sale>();
        public DbSet<SaleLine> SaleLines => Set<SaleLine>();
        public DbSet<SalePayment> SalePayments => Set<SalePayment>();
        public DbSet<ImportJob> ImportJobs => Set<ImportJob>();

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

            builder.Entity<Category>()
                .HasIndex(c => new { c.TenantId, c.Name, c.ParentId }).IsUnique();
            builder.Entity<Category>()
                .HasOne(c => c.Parent).WithMany().HasForeignKey(c => c.ParentId).OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Product>()
                .HasIndex(p => new { p.TenantId, p.Sku }).IsUnique().HasFilter("[Sku] IS NOT NULL");
            builder.Entity<Product>().HasIndex(p => new { p.TenantId, p.Barcode });
            builder.Entity<Product>()
                .HasOne(p => p.Category).WithMany().HasForeignKey(p => p.CategoryId).OnDelete(DeleteBehavior.SetNull);
            builder.Entity<Product>()
                .HasOne(p => p.TaxTreatment).WithMany().HasForeignKey(p => p.TaxTreatmentId).OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ProductVariant>()
                .HasOne(v => v.Product).WithMany(p => p.Variants).HasForeignKey(v => v.ProductId).OnDelete(DeleteBehavior.Cascade);
            builder.Entity<ProductVariant>()
                .HasIndex(v => new { v.TenantId, v.Sku }).IsUnique().HasFilter("[Sku] IS NOT NULL");

            builder.Entity<StockLevel>()
                .HasIndex(s => new { s.TenantId, s.ProductId, s.VariantId, s.LocationId, s.BatchId }).IsUnique();

            builder.Entity<StockMovement>()
                .HasIndex(m => new { m.TenantId, m.ProductId, m.OccurredAt });
            builder.Entity<StockMovement>()
                .HasIndex(m => new { m.TenantId, m.LocationId, m.OccurredAt });

            builder.Entity<Register>().HasIndex(r => new { r.TenantId, r.LocationId });

            builder.Entity<Shift>().HasIndex(s => new { s.TenantId, s.RegisterId, s.Status });

            builder.Entity<Sale>()
                .HasIndex(s => new { s.TenantId, s.ClientSaleId }).IsUnique();
            builder.Entity<Sale>().HasIndex(s => new { s.TenantId, s.OccurredAt });
            builder.Entity<Sale>()
                .HasMany(s => s.Lines).WithOne().HasForeignKey(l => l.SaleId).OnDelete(DeleteBehavior.Cascade);
            builder.Entity<Sale>()
                .HasMany(s => s.Payments).WithOne().HasForeignKey(p => p.SaleId).OnDelete(DeleteBehavior.Cascade);

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
