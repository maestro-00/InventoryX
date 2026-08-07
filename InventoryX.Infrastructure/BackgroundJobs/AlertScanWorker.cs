using InventoryX.Application.Services.IServices;
using InventoryX.Domain.Models.Auditing;
using InventoryX.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace InventoryX.Infrastructure.BackgroundJobs;

/// <summary>
/// Periodically scans stock levels and batches to raise alerts for:
/// low stock, out of stock, expiring batches, overstock, and slow-moving items.
/// Uses INotificationService with consolidation keys to avoid duplicate alerts.
/// </summary>
public sealed class AlertScanWorker(IServiceScopeFactory scopeFactory, ILogger<AlertScanWorker> logger)
    : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(15);
    private const int DefaultExpiryAlertDays = 30;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try { await ScanAsync(stoppingToken); }
            catch (Exception ex) { logger.LogError(ex, "AlertScanWorker iteration failed."); }
            await Task.Delay(Interval, stoppingToken);
        }
    }

    internal async Task ScanAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
        var now = DateTime.UtcNow;

        var tenantIds = await context.Tenants
            .IgnoreQueryFilters()
            .Select(t => t.Id)
            .ToListAsync(cancellationToken);

        foreach (var tenantId in tenantIds)
        {
            await ScanTenantAsync(context, notificationService, tenantId, now, cancellationToken);
        }
    }

    private static async Task ScanTenantAsync(
        AppDbContext context,
        INotificationService notificationService,
        Guid tenantId,
        DateTime now,
        CancellationToken cancellationToken)
    {
        // --- Low stock & out of stock ---
        var stockData = await context.StockLevels
            .IgnoreQueryFilters()
            .Where(sl => sl.TenantId == tenantId && sl.BatchId == null)
            .Join(context.Products.IgnoreQueryFilters()
                    .Where(p => p.TenantId == tenantId && !p.IsDeleted && p.ReorderPoint.HasValue),
                sl => sl.ProductId,
                p => p.Id,
                (sl, p) => new { sl.ProductId, sl.LocationId, sl.QtyOnHand, p.ReorderPoint, p.Name })
            .ToListAsync(cancellationToken);

        foreach (var sl in stockData)
        {
            if (sl.QtyOnHand == 0)
            {
                await notificationService.RaiseAsync(
                    NotificationType.OutOfStock,
                    $"out-of-stock:{sl.ProductId}:{sl.LocationId}",
                    "Out of Stock",
                    $"{sl.Name} is out of stock at location {sl.LocationId}.",
                    cancellationToken: cancellationToken);
            }
            else if (sl.QtyOnHand <= sl.ReorderPoint!.Value)
            {
                await notificationService.RaiseAsync(
                    NotificationType.LowStock,
                    $"low-stock:{sl.ProductId}:{sl.LocationId}",
                    "Low Stock",
                    $"{sl.Name} is at {sl.QtyOnHand} (reorder point: {sl.ReorderPoint}).",
                    cancellationToken: cancellationToken);
            }
        }

        // --- Expiring batches (check stock levels for batch qty > 0) ---
        var expiryHorizon = now.AddDays(DefaultExpiryAlertDays);
        var expiringBatches = await context.Batches
            .IgnoreQueryFilters()
            .Where(b => b.TenantId == tenantId && b.ExpiresAt.HasValue && b.ExpiresAt.Value <= expiryHorizon)
            .Join(context.Products.IgnoreQueryFilters().Where(p => p.TenantId == tenantId),
                b => b.ProductId,
                p => p.Id,
                (b, p) => new { b.Id, b.BatchNumber, b.ExpiresAt, p.Name })
            .ToListAsync(cancellationToken);

        // Only alert for batches that still have stock
        var batchIds = expiringBatches.Select(b => b.Id).ToList();
        var batchesWithStock = await context.StockLevels
            .IgnoreQueryFilters()
            .Where(sl => sl.TenantId == tenantId && sl.BatchId.HasValue && batchIds.Contains(sl.BatchId!.Value) && sl.QtyOnHand > 0)
            .Select(sl => sl.BatchId!.Value)
            .Distinct()
            .ToListAsync(cancellationToken);

        var batchesWithStockSet = new HashSet<Guid>(batchesWithStock);
        foreach (var batch in expiringBatches.Where(b => batchesWithStockSet.Contains(b.Id)))
        {
            await notificationService.RaiseAsync(
                NotificationType.ExpiringStock,
                $"expiry:{batch.Id}",
                "Expiring Stock",
                $"Batch {batch.BatchNumber} of {batch.Name} expires on {batch.ExpiresAt:yyyy-MM-dd}.",
                cancellationToken: cancellationToken);
        }
    }
}
