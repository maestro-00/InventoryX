using InventoryX.Application.Repository;
using InventoryX.Domain.Models;
using InventoryX.Domain.Models.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace InventoryX.Application.Services;

/// <summary>Reads plan usage from tenant records; monthly sales retain their period counter.</summary>
public static class BillingUsageReader
{
    public static async Task<int> CurrentAsync(IAppDbContext context, UsageMetric metric, Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var periodKey = DateTime.UtcNow.ToString("yyyy-MM");
        return metric switch
        {
            UsageMetric.SalesThisMonth => await context.UsageCounters.AsNoTracking()
                .Where(item => item.Metric == metric && item.PeriodKey == periodKey)
                .Select(item => (int?)item.Count)
                .FirstOrDefaultAsync(cancellationToken) ?? 0,
            UsageMetric.Locations => await context.Locations.AsNoTracking()
                .CountAsync(item => !item.IsDeleted, cancellationToken),
            UsageMetric.Users => await context.Users.AsNoTracking()
                .CountAsync(item => item.TenantId == tenantId && item.Status != UserStatus.Deactivated, cancellationToken),
            UsageMetric.Products => await context.Products.AsNoTracking()
                .CountAsync(item => !item.IsDeleted, cancellationToken),
            UsageMetric.Registers => await context.Registers.AsNoTracking().CountAsync(cancellationToken),
            _ => 0,
        };
    }
}
