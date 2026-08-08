using InventoryX.Domain.Models.Catalog;
using InventoryX.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace InventoryX.Infrastructure.BackgroundJobs;

public sealed class RetentionWorker(IServiceScopeFactory scopes, ILogger<RetentionWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopes.CreateScope();
                await scope.ServiceProvider.GetRequiredService<RetentionProcessor>().ProcessAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception error) { logger.LogError(error, "History retention scan failed"); }
            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }
}

public sealed class RetentionProcessor(AppDbContext context)
{
    public async Task ProcessAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var subscriptions = await context.Subscriptions.IgnoreQueryFilters().Include(item => item.Plan)
            .Where(item => item.Plan != null && item.Status != Domain.Models.Tenancy.SubscriptionStatus.PurgePending)
            .ToListAsync(cancellationToken);

        foreach (var subscription in subscriptions)
        {
            var historyMonths = subscription.Plan!.HistoryMonths;
            if (historyMonths is null) continue;
            var cutoff = now.AddMonths(-historyMonths.Value);
            var notifications = await context.Notifications.IgnoreQueryFilters()
                .Where(item => item.TenantId == subscription.TenantId && item.LastRaisedAt < cutoff).ToListAsync(cancellationToken);
            context.Notifications.RemoveRange(notifications);
            var digestDeliveries = await context.NotificationDigestDeliveries.IgnoreQueryFilters()
                .Where(item => item.TenantId == subscription.TenantId && item.CreatedAt < cutoff).ToListAsync(cancellationToken);
            context.NotificationDigestDeliveries.RemoveRange(digestDeliveries);
            var exportJobs = await context.ReportExportJobs.IgnoreQueryFilters()
                .Where(item => item.TenantId == subscription.TenantId && item.RequestedAt < cutoff).ToListAsync(cancellationToken);
            context.ReportExportJobs.RemoveRange(exportJobs);
        }

        var expiredProducts = await context.Products.IgnoreQueryFilters()
            .Where(item => item.IsDeleted && item.RecoveryExpiresAt != null && item.RecoveryExpiresAt < now).ToListAsync(cancellationToken);
        var expiredCategories = await context.Categories.IgnoreQueryFilters()
            .Where(item => item.IsDeleted && item.RecoveryExpiresAt != null && item.RecoveryExpiresAt < now).ToListAsync(cancellationToken);
        var expiredLocations = await context.Locations.IgnoreQueryFilters()
            .Where(item => item.IsDeleted && item.RecoveryExpiresAt != null && item.RecoveryExpiresAt < now).ToListAsync(cancellationToken);
        context.Products.RemoveRange(expiredProducts);
        context.Categories.RemoveRange(expiredCategories);
        context.Locations.RemoveRange(expiredLocations);
        await context.SaveChangesAsync(cancellationToken);
    }
}
