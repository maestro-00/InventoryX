using FluentAssertions;
using InventoryX.Application.Queries.RequestHandlers.Selling;
using InventoryX.Application.Queries.Requests.Selling;
using InventoryX.Application.Services;
using InventoryX.Common.Tests;
using InventoryX.Domain.Models.Inventory;
using InventoryX.Domain.Models.Selling;

namespace InventoryX.Application.Tests.Selling;

/// <summary>T086 - end-of-shift report must aggregate tender, refund, discount, void and variance totals.</summary>
public sealed class ZReportTests : IDisposable
{
    private readonly TestDb _db = new(Guid.NewGuid(), "cashier-1");

    [Fact]
    public void Z_report_query_contract_exposes_register_and_staff_aggregation()
    {
        Type.GetType("InventoryX.Application.Queries.Requests.Selling.GetZReportQuery, InventoryX.Application")
            .Should().NotBeNull("a shift must expose a Z-report with sales, tender, refund, discount, void, and variance totals");
        Type.GetType("InventoryX.Application.Queries.RequestHandlers.Selling.GetZReportQueryHandler, InventoryX.Application")
            .Should().NotBeNull();
    }

    [Fact]
    public async Task Open_shift_report_calculates_expected_cash_before_close()
    {
        await using var context = _db.CreateContext();
        var location = new Location { Name = "Main Shop" };
        var register = new Register { Name = "Counter 1", LocationId = location.Id };
        var shift = new Shift
        {
            RegisterId = register.Id,
            OpenedBy = "cashier-1",
            OpenedAt = DateTime.UtcNow,
            OpeningFloat = 100m,
        };
        var sale = new Sale
        {
            LocationId = location.Id,
            RegisterId = register.Id,
            ShiftId = shift.Id,
            CashierId = "cashier-1",
            ClientSaleId = Guid.NewGuid(),
            OccurredAt = DateTime.UtcNow,
            GrandTotal = 75m,
            ChangeGiven = 5m,
        };
        context.AddRange(location, register, shift, sale);
        context.SalePayments.AddRange(
            new SalePayment { SaleId = sale.Id, Tender = TenderType.Cash, Amount = 30m },
            new SalePayment { SaleId = sale.Id, Tender = TenderType.Card, Amount = 50m });
        context.CashMovements.AddRange(
            new CashMovement { ShiftId = shift.Id, Type = CashMovementType.CashIn, Amount = 10m, Reason = "ChangeOrder", RecordedBy = "cashier-1" },
            new CashMovement { ShiftId = shift.Id, Type = CashMovementType.CashOut, Amount = 3m, Reason = "PettyCash", RecordedBy = "cashier-1" });
        await context.SaveChangesAsync();

        var access = TestPosAccess.Cashier(context, _db.TenantContext);
        var report = await new GetZReportQueryHandler(context, access).Handle(
            new GetZReportQuery(shift.Id), CancellationToken.None);

        report.ExpectedCash.Should().Be(132m);
        report.CashTendered.Should().Be(30m);
        report.CountedCash.Should().BeNull();
        report.Variance.Should().BeNull();
    }

    public void Dispose() => _db.Dispose();
}
