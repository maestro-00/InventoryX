using FluentAssertions;
using InventoryX.Application.Commands.RequestHandlers.Inventory;
using InventoryX.Application.Commands.Requests.Inventory;
using InventoryX.Common.Tests;
using InventoryX.Domain.Models.Catalog;
using InventoryX.Domain.Models.Inventory;
using InventoryX.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace InventoryX.Application.Tests.Inventory;

public sealed class MovementCorrectionTests : IDisposable
{
    private readonly TestDb _db = new(Guid.NewGuid(), "manager-1");

    [Fact]
    public async Task Correction_keeps_original_and_appends_only_the_difference()
    {
        await using var context = _db.CreateContext();
        var location = new Location { Name = "Main" };
        var product = new Product { Name = "Sugar", SellingPrice = 10m };
        context.AddRange(location, product);
        await context.SaveChangesAsync();
        var ledger = new StockLedger(context);
        await ledger.AppendAsync([new StockMovementRequest(MovementType.Adjustment, product.Id, location.Id, 10m)]);
        await context.SaveChangesAsync();
        var original = await context.StockMovements.SingleAsync();

        var result = await new CorrectMovementCommandHandler(context, ledger).Handle(new CorrectMovementCommand
        {
            MovementId = original.Id,
            CorrectedQtyDelta = 8m,
            Note = "Opening count was overstated",
        }, CancellationToken.None);

        result.QtyDelta.Should().Be(-2m);
        (await context.StockMovements.FindAsync(original.Id))!.QtyDelta.Should().Be(10m);
        (await context.StockLevels.SingleAsync()).QtyOnHand.Should().Be(8m);
        (await context.StockMovements.CountAsync()).Should().Be(2);
    }

    public void Dispose() => _db.Dispose();
}
