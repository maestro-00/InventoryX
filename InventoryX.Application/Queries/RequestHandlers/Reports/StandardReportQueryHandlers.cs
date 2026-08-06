using System.Text.Json;
using InventoryX.Application.Queries.Requests.Reports;
using InventoryX.Application.Repository;
using InventoryX.Application.Services.IServices;
using InventoryX.Domain.Models.Selling;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InventoryX.Application.Queries.RequestHandlers.Reports;

file static class ReportData
{
    public static void Validate(ReportFilter filter)
    {
        if (filter.To <= filter.From) throw new FluentValidation.ValidationException("Report 'to' must be later than 'from'.");
    }

    public static async Task<List<Sale>> Sales(IAppDbContext context, ReportFilter filter, CancellationToken cancellationToken)
    {
        Validate(filter);
        var query = context.Sales.AsNoTracking().Include(sale => sale.Lines).Include(sale => sale.Payments)
            .Where(sale => sale.OccurredAt >= filter.From && sale.OccurredAt < filter.To);
        if (filter.LocationId is Guid locationId) query = query.Where(sale => sale.LocationId == locationId);
        if (!string.IsNullOrWhiteSpace(filter.StaffId)) query = query.Where(sale => sale.CashierId == filter.StaffId);
        var sales = await query.ToListAsync(cancellationToken);
        if (filter.CategoryId is not Guid categoryId) return sales;
        var productIds = await context.Products.AsNoTracking().Where(product => product.CategoryId == categoryId)
            .Select(product => product.Id).ToListAsync(cancellationToken);
        return sales.Where(sale => sale.Lines.Any(line => productIds.Contains(line.ProductId))).ToList();
    }
}

public sealed class GetSalesReportQueryHandler(IAppDbContext context) : IRequestHandler<GetSalesReportQuery, SalesReportDto>
{
    public async Task<SalesReportDto> Handle(GetSalesReportQuery request, CancellationToken cancellationToken)
    {
        var sales = await ReportData.Sales(context, request.Filter, cancellationToken);
        var rows = sales.OrderBy(sale => sale.OccurredAt).Select(sale => new SalesReportRowDto(sale.Id, sale.OccurredAt,
            sale.LocationId, sale.CashierId, sale.Subtotal, sale.DiscountTotal, sale.TaxTotal, sale.GrandTotal, sale.Status.ToString())).ToList();
        return new SalesReportDto(request.Filter, rows, sales.Where(sale => sale.Status == SaleStatus.Completed).Sum(sale => sale.GrandTotal),
            sales.Count(sale => sale.Status == SaleStatus.Completed));
    }
}

public sealed class GetProfitReportQueryHandler(IAppDbContext context) : IRequestHandler<GetProfitReportQuery, ProfitReportDto>
{
    public async Task<ProfitReportDto> Handle(GetProfitReportQuery request, CancellationToken cancellationToken)
    {
        var sales = (await ReportData.Sales(context, request.Filter, cancellationToken)).Where(sale => sale.Status == SaleStatus.Completed).ToList();
        var productIds = sales.SelectMany(sale => sale.Lines).Select(line => line.ProductId).Distinct().ToList();
        var products = await context.Products.AsNoTracking().Where(product => productIds.Contains(product.Id))
            .ToDictionaryAsync(product => product.Id, cancellationToken);
        var rows = sales.SelectMany(sale => sale.Lines).GroupBy(line => line.ProductId).Select(group =>
        {
            products.TryGetValue(group.Key, out var product);
            var revenue = group.Sum(line => line.LineTotal - line.TaxAmount);
            var cost = group.Sum(line => line.Qty) * (product?.CostPrice ?? 0m);
            return new ProfitReportRowDto(group.Key, product?.Name ?? group.First().ProductName, revenue, cost, revenue - cost);
        }).OrderByDescending(row => row.GrossProfit).ToList();
        return new ProfitReportDto(request.Filter, rows, rows.Sum(row => row.GrossProfit));
    }
}

public sealed class GetStockReportQueryHandler(IAppDbContext context) : IRequestHandler<GetStockReportQuery, StockReportDto>
{
    public async Task<StockReportDto> Handle(GetStockReportQuery request, CancellationToken cancellationToken)
    {
        ReportData.Validate(request.Filter);
        var query = context.StockLevels.AsNoTracking().AsQueryable();
        if (request.Filter.LocationId is Guid locationId) query = query.Where(level => level.LocationId == locationId);
        var levels = await query.ToListAsync(cancellationToken);
        var productIds = levels.Select(level => level.ProductId).Distinct().ToList();
        var productsQuery = context.Products.AsNoTracking().Where(product => productIds.Contains(product.Id));
        if (request.Filter.CategoryId is Guid categoryId) productsQuery = productsQuery.Where(product => product.CategoryId == categoryId);
        var products = await productsQuery.ToDictionaryAsync(product => product.Id, cancellationToken);
        var rows = levels.Where(level => products.ContainsKey(level.ProductId)).Select(level => new StockReportRowDto(level.ProductId,
            products[level.ProductId].Name, level.LocationId, level.QtyOnHand, level.AvgUnitCost,
            Math.Round(level.QtyOnHand * level.AvgUnitCost, 4))).ToList();
        return new StockReportDto(request.Filter, rows, rows.Sum(row => row.Value));
    }
}

public sealed class GetPurchasingReportQueryHandler(IAppDbContext context) : IRequestHandler<GetPurchasingReportQuery, PurchasingReportDto>
{
    public async Task<PurchasingReportDto> Handle(GetPurchasingReportQuery request, CancellationToken cancellationToken)
    {
        ReportData.Validate(request.Filter);
        var orders = await context.PurchaseOrders.AsNoTracking().Include(order => order.Supplier).Include(order => order.Lines)
            .Where(order => order.CreatedAt >= request.Filter.From && order.CreatedAt < request.Filter.To).ToListAsync(cancellationToken);
        if (request.Filter.LocationId is Guid locationId) orders = orders.Where(order => order.DeliverToLocationId == locationId).ToList();
        var rows = orders.Select(order => new PurchasingReportRowDto(order.Id, order.SupplierId, order.Supplier?.Name ?? "Unknown",
            order.Status.ToString(), order.RequiredBy, order.Total, order.Lines.Sum(line => Math.Max(0, line.OrderedQty - line.ReceivedQty)))).ToList();
        return new PurchasingReportDto(request.Filter, rows);
    }
}

public sealed class GetStaffReportQueryHandler(IAppDbContext context) : IRequestHandler<GetStaffReportQuery, StaffReportDto>
{
    public async Task<StaffReportDto> Handle(GetStaffReportQuery request, CancellationToken cancellationToken)
    {
        var sales = await ReportData.Sales(context, request.Filter, cancellationToken);
        var rows = sales.GroupBy(sale => sale.CashierId).Select(group => new StaffReportRowDto(group.Key,
            group.Count(sale => sale.Status == SaleStatus.Completed), group.Where(sale => sale.Status == SaleStatus.Completed).Sum(sale => sale.GrandTotal),
            group.Sum(sale => sale.DiscountTotal), group.Count(sale => sale.Status == SaleStatus.Voided))).ToList();
        return new StaffReportDto(request.Filter, rows);
    }
}

public sealed class GetTaxReportQueryHandler(IAppDbContext context) : IRequestHandler<GetTaxReportQuery, TaxReportDto>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    public async Task<TaxReportDto> Handle(GetTaxReportQuery request, CancellationToken cancellationToken)
    {
        var sales = (await ReportData.Sales(context, request.Filter, cancellationToken)).Where(sale => sale.Status == SaleStatus.Completed);
        var components = sales.SelectMany(sale => sale.Lines).SelectMany(line =>
            JsonSerializer.Deserialize<List<TaxComponentResult>>(line.TaxComponents, JsonOptions) ?? []).ToList();
        var rows = components.GroupBy(component => new { component.Code, component.Name, component.Rate })
            .Select(group => new TaxReportComponentDto(group.Key.Code, group.Key.Name, group.Key.Rate, group.Sum(component => component.Amount)))
            .OrderBy(row => row.Code).ThenBy(row => row.Rate).ToList();
        return new TaxReportDto(request.From, request.To, rows, rows.Sum(row => row.Amount));
    }
}
