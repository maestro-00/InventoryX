using FluentAssertions;
using InventoryX.Application.Queries.RequestHandlers.Reports;
using InventoryX.Application.Queries.Requests.Reports;
using InventoryX.Common.Tests;
using InventoryX.Domain.Models.Catalog;
using InventoryX.Domain.Models.Inventory;
using InventoryX.Domain.Models.Selling;

namespace InventoryX.Application.Tests.Reports;

public sealed class StandardReportQueryTests : IDisposable
{
    private readonly TestDb _db = new(Guid.NewGuid());

    [Fact]
    public async Task Sales_report_applies_period_location_and_staff_filters()
    {
        await using var context = _db.CreateContext();
        var now = DateTime.UtcNow;
        var location = Guid.NewGuid();
        context.Sales.AddRange(Sale(now, location, "alice", 10m), Sale(now, Guid.NewGuid(), "alice", 20m), Sale(now, location, "bob", 30m));
        await context.SaveChangesAsync();

        var result = await new GetSalesReportQueryHandler(context).Handle(
            new GetSalesReportQuery(new ReportFilter(now.AddHours(-1), now.AddHours(1), location, StaffId: "alice")), CancellationToken.None);

        result.Rows.Should().ContainSingle().Which.Total.Should().Be(10m);
    }

    [Fact]
    public async Task Stock_report_uses_maintained_weighted_average_cost_for_valuation()
    {
        await using var context = _db.CreateContext();
        var product = new Product { Name = "Sugar" };
        context.Products.Add(product);
        context.StockLevels.Add(new StockLevel { ProductId = product.Id, LocationId = Guid.NewGuid(), QtyOnHand = 5m, AvgUnitCost = 3m });
        await context.SaveChangesAsync();

        var result = await new GetStockReportQueryHandler(context).Handle(
            new GetStockReportQuery(new ReportFilter(DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1))), CancellationToken.None);

        result.Rows.Should().ContainSingle().Which.Value.Should().Be(15m);
        result.TotalValue.Should().Be(15m);
    }

    private static Sale Sale(DateTime when, Guid location, string staff, decimal total) => new()
    {
        LocationId = location, RegisterId = Guid.NewGuid(), ShiftId = Guid.NewGuid(), CashierId = staff,
        ClientSaleId = Guid.NewGuid(), OccurredAt = when, GrandTotal = total, Status = SaleStatus.Completed,
    };

    public void Dispose() => _db.Dispose();
}
