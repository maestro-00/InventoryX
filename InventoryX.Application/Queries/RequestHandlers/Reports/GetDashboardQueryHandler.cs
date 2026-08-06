using InventoryX.Application.Queries.Requests.Reports;
using InventoryX.Application.Repository;
using InventoryX.Domain.Models.Selling;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InventoryX.Application.Queries.RequestHandlers.Reports;

public sealed class GetDashboardQueryHandler(IAppDbContext context) : IRequestHandler<GetDashboardQuery, DashboardDto>
{
    public async Task<DashboardDto> Handle(GetDashboardQuery request, CancellationToken cancellationToken)
    {
        var asOf = request.AsOf ?? DateTime.UtcNow;
        var today = asOf.Date;
        var tomorrow = today.AddDays(1);
        var prior = today.AddDays(-7);
        var priorEnd = prior.AddDays(1);
        var sales = await context.Sales.AsNoTracking()
            .Where(sale => sale.Status == SaleStatus.Completed && sale.OccurredAt >= prior && sale.OccurredAt < tomorrow)
            .Include(sale => sale.Lines).Include(sale => sale.Payments).ToListAsync(cancellationToken);
        var current = sales.Where(sale => sale.OccurredAt >= today && sale.OccurredAt < tomorrow).ToList();
        var previous = sales.Where(sale => sale.OccurredAt >= prior && sale.OccurredAt < priorEnd).ToList();
        static decimal Total(IEnumerable<Sale> values) => values.Sum(sale => sale.GrandTotal);
        static int Count(IEnumerable<Sale> values) => values.Count();
        static decimal Items(IEnumerable<Sale> values) => values.SelectMany(sale => sale.Lines).Sum(line => line.Qty);
        static decimal Average(IReadOnlyCollection<Sale> values) => values.Count == 0 ? 0 : Math.Round(Total(values) / values.Count, 2);

        var products = await context.Products.AsNoTracking().ToDictionaryAsync(product => product.Id, cancellationToken);
        var levels = await context.StockLevels.AsNoTracking().ToListAsync(cancellationToken);
        var lowStock = products.Values.Count(product => product.ReorderPoint is decimal point &&
            levels.Where(level => level.ProductId == product.Id).Sum(level => level.QtyOnHand) <= point);
        var expiringBatchIds = await context.Batches.AsNoTracking()
            .Where(batch => batch.ExpiresAt >= today && batch.ExpiresAt < today.AddDays(30)).Select(batch => batch.Id).ToListAsync(cancellationToken);
        var expiryWarnings = levels.Count(level => level.BatchId != null && expiringBatchIds.Contains(level.BatchId.Value) && level.QtyOnHand > 0);
        var topSellers = current.SelectMany(sale => sale.Lines).GroupBy(line => new { line.ProductId, line.ProductName })
            .Select(group => new DashboardTopSellerDto(group.Key.ProductId, group.Key.ProductName,
                group.Sum(line => line.Qty), group.Sum(line => line.LineTotal), $"/api/v1/reports/sales?productId={group.Key.ProductId}"))
            .OrderByDescending(item => item.Quantity).Take(10).ToList();
        var currentCash = current.SelectMany(sale => sale.Payments).Where(payment => payment.Tender == TenderType.Cash).Sum(payment => payment.Amount)
            - current.Sum(sale => sale.ChangeGiven);
        var previousCash = previous.SelectMany(sale => sale.Payments).Where(payment => payment.Tender == TenderType.Cash).Sum(payment => payment.Amount)
            - previous.Sum(sale => sale.ChangeGiven);
        decimal? profit = request.IncludeProfit
            ? current.SelectMany(sale => sale.Lines).Sum(line => line.LineTotal - line.Qty *
                (products.TryGetValue(line.ProductId, out var product) ? product.CostPrice : 0m))
            : null;

        return new DashboardDto(
            new DashboardMetricDto(Total(current), Total(previous), "/api/v1/reports/sales"),
            new DashboardCountMetricDto(Count(current), Count(previous), "/api/v1/reports/sales"),
            new DashboardMetricDto(Average(current), Average(previous), "/api/v1/reports/sales"),
            new DashboardMetricDto(Items(current), Items(previous), "/api/v1/reports/sales"),
            new DashboardMetricDto(currentCash, previousCash, "/api/v1/reports/staff"),
            lowStock, expiryWarnings, topSellers, profit);
    }
}

// T105 remains intentionally red until T107 implements the report suite.
public sealed class GetTaxReportQueryHandler(IAppDbContext context) : IRequestHandler<GetTaxReportQuery, TaxReportDto>
{
    public Task<TaxReportDto> Handle(GetTaxReportQuery request, CancellationToken cancellationToken) =>
        throw new NotImplementedException("Tax reporting is implemented by T107.");
}
