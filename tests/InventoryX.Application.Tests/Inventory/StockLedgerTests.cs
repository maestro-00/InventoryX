using FluentAssertions;
using InventoryX.Application.Exceptions;
using InventoryX.Application.Services.IServices;
using InventoryX.Common.Tests;
using InventoryX.Domain.Models.Inventory;
using InventoryX.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace InventoryX.Application.Tests.Inventory;

/// <summary>
/// T029 — the ledger must append immutable movements and keep the StockLevel
/// projection (incl. weighted-average cost) in the same unit of work.
/// </summary>
public sealed class StockLedgerTests : IDisposable
{
    private static readonly Guid Tenant = Guid.NewGuid();
    private readonly TestDb _db = new(Tenant);
    private readonly Guid _productId = Guid.NewGuid();
    private readonly Guid _locationId = Guid.NewGuid();

    [Fact]
    public async Task Opening_stock_creates_movement_and_projection()
    {
        await using var context = _db.CreateContext();
        var ledger = new StockLedger(context);

        await ledger.AppendAsync([
            new StockMovementRequest(MovementType.Adjustment, _productId, _locationId, 10m, UnitCost: 6m, ReasonCode: "Correction"),
        ]);
        await context.SaveChangesAsync();

        var level = await context.StockLevels.SingleAsync();
        level.QtyOnHand.Should().Be(10m);
        level.AvgUnitCost.Should().Be(6m);

        var movement = await context.StockMovements.SingleAsync();
        movement.QtyDelta.Should().Be(10m);
        movement.ReasonCode.Should().Be("Correction");
    }

    [Fact]
    public async Task Sale_decrements_projection_and_appends_second_movement()
    {
        await using var context = _db.CreateContext();
        var ledger = new StockLedger(context);

        await ledger.AppendAsync([new StockMovementRequest(MovementType.Adjustment, _productId, _locationId, 10m, UnitCost: 6m)]);
        await context.SaveChangesAsync();

        await ledger.AppendAsync([new StockMovementRequest(MovementType.Sale, _productId, _locationId, -2m)]);
        await context.SaveChangesAsync();

        (await context.StockLevels.SingleAsync()).QtyOnHand.Should().Be(8m);
        (await context.StockMovements.CountAsync()).Should().Be(2);
    }

    [Fact]
    public async Task Weighted_average_cost_updates_on_receipt()
    {
        await using var context = _db.CreateContext();
        var ledger = new StockLedger(context);

        await ledger.AppendAsync([new StockMovementRequest(MovementType.Receipt, _productId, _locationId, 10m, UnitCost: 6m)]);
        await context.SaveChangesAsync();
        await ledger.AppendAsync([new StockMovementRequest(MovementType.Receipt, _productId, _locationId, 10m, UnitCost: 8m)]);
        await context.SaveChangesAsync();

        (await context.StockLevels.SingleAsync()).AvgUnitCost.Should().Be(7m);
    }

    [Fact]
    public async Task Oversell_without_allow_negative_is_rejected()
    {
        await using var context = _db.CreateContext();
        var ledger = new StockLedger(context);

        await ledger.AppendAsync([new StockMovementRequest(MovementType.Adjustment, _productId, _locationId, 1m, UnitCost: 6m)]);
        await context.SaveChangesAsync();

        var act = () => ledger.AppendAsync([new StockMovementRequest(MovementType.Sale, _productId, _locationId, -5m)]);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Oversell_with_allow_negative_goes_through_for_offline_ingest()
    {
        await using var context = _db.CreateContext();
        var ledger = new StockLedger(context);

        await ledger.AppendAsync([new StockMovementRequest(MovementType.Adjustment, _productId, _locationId, 1m, UnitCost: 6m)]);
        await context.SaveChangesAsync();

        await ledger.AppendAsync([new StockMovementRequest(MovementType.Sale, _productId, _locationId, -5m, AllowNegative: true)]);
        await context.SaveChangesAsync();

        (await context.StockLevels.SingleAsync()).QtyOnHand.Should().Be(-4m);
    }

    public void Dispose() => _db.Dispose();
}
