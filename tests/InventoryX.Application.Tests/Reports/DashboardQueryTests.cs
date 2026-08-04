using FluentAssertions;
using InventoryX.Application.Queries.RequestHandlers.Reports;
using InventoryX.Application.Queries.Requests.Reports;
using InventoryX.Common.Tests;
using InventoryX.Domain.Models.Selling;

namespace InventoryX.Application.Tests.Reports;

public sealed class DashboardQueryTests : IDisposable
{
    private readonly TestDb _db = new(Guid.NewGuid());

    [Fact]
    public async Task Dashboard_compares_today_with_same_day_last_week_and_ignores_voided_sales()
    {
        await using var context = _db.CreateContext();
        var now = new DateTime(2026, 8, 4, 12, 0, 0, DateTimeKind.Utc);
        context.Sales.AddRange(
            SaleAt(now.AddHours(-2), 120m, 3m),
            SaleAt(now.AddDays(-7).AddHours(-1), 80m, 2m),
            SaleAt(now.AddHours(-1), 999m, 9m, SaleStatus.Voided));
        await context.SaveChangesAsync();

        var result = await new GetDashboardQueryHandler(context).Handle(
            new GetDashboardQuery(now, IncludeProfit: true), CancellationToken.None);

        result.Sales.Today.Should().Be(120m);
        result.Sales.SameDayLastWeek.Should().Be(80m);
        result.TransactionCount.Today.Should().Be(1);
        result.AverageBasket.Today.Should().Be(120m);
        result.ItemsSold.Today.Should().Be(3m);
        result.Sales.DetailUrl.Should().Be("/api/v1/reports/sales");
    }

    [Fact]
    public async Task Dashboard_redacts_profit_when_view_profit_is_not_granted()
    {
        await using var context = _db.CreateContext();
        var now = DateTime.UtcNow;
        context.Sales.Add(SaleAt(now, 100m, 1m));
        await context.SaveChangesAsync();
        var handler = new GetDashboardQueryHandler(context);

        var redacted = await handler.Handle(new GetDashboardQuery(now, IncludeProfit: false), CancellationToken.None);
        var allowed = await handler.Handle(new GetDashboardQuery(now, IncludeProfit: true), CancellationToken.None);

        redacted.GrossProfit.Should().BeNull();
        allowed.GrossProfit.Should().NotBeNull();
    }

    private static Sale SaleAt(DateTime occurredAt, decimal total, decimal quantity, SaleStatus status = SaleStatus.Completed) => new()
    {
        LocationId = Guid.NewGuid(), RegisterId = Guid.NewGuid(), ShiftId = Guid.NewGuid(), CashierId = "cashier",
        ClientSaleId = Guid.NewGuid(), OccurredAt = occurredAt, Status = status, GrandTotal = total, Subtotal = total,
        Lines = [new SaleLine { ProductId = Guid.NewGuid(), ProductName = "Item", Qty = quantity, UnitPrice = total / quantity, LineTotal = total }],
    };

    public void Dispose() => _db.Dispose();
}
