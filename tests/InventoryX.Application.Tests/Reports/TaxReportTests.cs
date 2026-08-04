using FluentAssertions;
using InventoryX.Application.Queries.RequestHandlers.Reports;
using InventoryX.Application.Queries.Requests.Reports;
using InventoryX.Common.Tests;
using InventoryX.Domain.Models.Selling;

namespace InventoryX.Application.Tests.Reports;

public sealed class TaxReportTests : IDisposable
{
    private readonly TestDb _db = new(Guid.NewGuid());

    [Fact]
    public async Task Ghana_tax_report_sums_snapshot_components_by_code_and_rate_within_period()
    {
        await using var context = _db.CreateContext();
        var from = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc);
        context.Sales.AddRange(
            SaleWithTax(from.AddHours(2), """[{"code":"NHIL","name":"NHIL","rate":0.025,"amount":2.5},{"code":"VAT","name":"VAT","rate":0.15,"amount":15.9}]"""),
            SaleWithTax(from.AddDays(1), """[{"code":"NHIL","name":"NHIL","rate":0.025,"amount":1.25},{"code":"VAT","name":"VAT","rate":0.15,"amount":7.95}]"""),
            SaleWithTax(from.AddDays(-1), """[{"code":"VAT","name":"VAT","rate":0.15,"amount":999}]"""));
        await context.SaveChangesAsync();

        var result = await new GetTaxReportQueryHandler(context).Handle(
            new GetTaxReportQuery(from, from.AddDays(2)), CancellationToken.None);

        result.Components.Single(item => item.Code == "NHIL" && item.Rate == 0.025m).Amount.Should().Be(3.75m);
        result.Components.Single(item => item.Code == "VAT" && item.Rate == 0.15m).Amount.Should().Be(23.85m);
        result.TotalTax.Should().Be(27.60m);
    }

    private static Sale SaleWithTax(DateTime occurredAt, string components) => new()
    {
        LocationId = Guid.NewGuid(), RegisterId = Guid.NewGuid(), ShiftId = Guid.NewGuid(), CashierId = "cashier",
        ClientSaleId = Guid.NewGuid(), OccurredAt = occurredAt, Status = SaleStatus.Completed,
        Lines = [new SaleLine { ProductId = Guid.NewGuid(), ProductName = "Item", Qty = 1m, TaxComponents = components }],
    };

    public void Dispose() => _db.Dispose();
}
