using FluentAssertions;
using InventoryX.Application.Commands.RequestHandlers.Inventory;
using InventoryX.Application.Commands.Requests.Inventory;
using InventoryX.Common.Tests;
using InventoryX.Domain.Models.Catalog;
using InventoryX.Domain.Models.Inventory;
using InventoryX.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace InventoryX.Application.Tests.Inventory;

public sealed class ConsumptionTests : IDisposable
{
    private readonly TestDb _db = new(Guid.NewGuid(), "stock-clerk-1");

    [Fact]
    public async Task Consumption_posts_an_auditable_negative_stock_movement()
    {
        await using var context = _db.CreateContext();
        var location = new Location { Name = "Kitchen" };
        var product = new Product { Name = "Sugar", SellingPrice = 10m };
        context.AddRange(location, product);
        await context.SaveChangesAsync();
        var ledger = new StockLedger(context);
        await ledger.AppendAsync([new StockMovementRequest(MovementType.Adjustment, product.Id, location.Id, 10m)]);
        await context.SaveChangesAsync();

        var handler = new RecordConsumptionCommandHandler(context, ledger);
        await handler.Handle(new RecordConsumptionCommand
        {
            LocationId = location.Id,
            ReasonCode = "Sample",
            Lines = [new AdjustmentLineDto { ProductId = product.Id, QtyDelta = 3m }],
        }, CancellationToken.None);

        (await context.StockLevels.SingleAsync()).QtyOnHand.Should().Be(7m);
        var movement = await context.StockMovements.OrderByDescending(m => m.OccurredAt).FirstAsync();
        movement.Type.Should().Be(MovementType.Consumption);
        movement.QtyDelta.Should().Be(-3m);
        movement.ReasonCode.Should().Be("Sample");
    }

    public void Dispose() => _db.Dispose();
}
