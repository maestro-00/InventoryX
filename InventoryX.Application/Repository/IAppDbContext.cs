using InventoryX.Domain.Models.Auditing;
using InventoryX.Domain.Models.Catalog;
using InventoryX.Domain.Models.Inventory;
using InventoryX.Domain.Models.Selling;
using InventoryX.Domain.Models.Tenancy;
using InventoryX.Domain.Models;
using InventoryX.Domain.Models.Purchasing;
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
        DbSet<User> Users { get; }
        DbSet<PlanDefinition> PlanDefinitions { get; }
        DbSet<Subscription> Subscriptions { get; }
        DbSet<BillingInvoice> BillingInvoices { get; }
        DbSet<UsageCounter> UsageCounters { get; }
        DbSet<RegisterPin> RegisterPins { get; }
        DbSet<Role> AppRoles { get; }
        DbSet<AuditLogEntry> AuditLogEntries { get; }
        DbSet<Notification> Notifications { get; }
        DbSet<TaxTreatment> TaxTreatments { get; }

        DbSet<Location> Locations { get; }
        DbSet<Category> Categories { get; }
        DbSet<Product> Products { get; }
        DbSet<ProductVariant> ProductVariants { get; }
        DbSet<StockLevel> StockLevels { get; }
        DbSet<StockMovement> StockMovements { get; }
        DbSet<StockTransfer> StockTransfers { get; }
        DbSet<StockTransferLine> StockTransferLines { get; }
        DbSet<StockAdjustment> StockAdjustments { get; }
        DbSet<StockAdjustmentLine> StockAdjustmentLines { get; }
        DbSet<AdjustmentReason> AdjustmentReasons { get; }
        DbSet<StockCount> StockCounts { get; }
        DbSet<StockCountLine> StockCountLines { get; }
        DbSet<Register> Registers { get; }
        DbSet<FavouritesLayout> FavouritesLayouts { get; }
        DbSet<Shift> Shifts { get; }
        DbSet<CashMovement> CashMovements { get; }
        DbSet<Sale> Sales { get; }
        DbSet<SaleLine> SaleLines { get; }
        DbSet<SalePayment> SalePayments { get; }
        DbSet<Receipt> Receipts { get; }
        DbSet<ReceiptDeliveryLog> ReceiptDeliveryLogs { get; }
        DbSet<ReturnTransaction> ReturnTransactions { get; }
        DbSet<ReturnLine> ReturnLines { get; }
        DbSet<ImportJob> ImportJobs { get; }
        DbSet<Supplier> Suppliers { get; }
        DbSet<PurchaseOrder> PurchaseOrders { get; }
        DbSet<PurchaseOrderLine> PurchaseOrderLines { get; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
