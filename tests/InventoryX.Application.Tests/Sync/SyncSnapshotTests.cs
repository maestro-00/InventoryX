using FluentAssertions;
using InventoryX.Application.Queries.RequestHandlers.Sync;
using InventoryX.Application.Queries.Requests.Sync;
using InventoryX.Common.Tests;
using InventoryX.Domain.Models.Catalog;
using InventoryX.Domain.Models.Inventory;
using InventoryX.Domain.Models.Selling;
using InventoryX.Infrastructure.Services;

namespace InventoryX.Application.Tests.Sync;

public sealed class SyncSnapshotTests : IDisposable
{
    private readonly TestDb _db = new(Guid.NewGuid(), "register-1");

    [Fact]
    public async Task Watermark_returns_only_changes_for_the_register_location()
    {
        await using var context = _db.CreateContext();
        var location = new Location { Name = "Main" };
        var other = new Location { Name = "Other" };
        var register = new Register { Name = "R1", LocationId = location.Id };
        var product = new Product { Name = "Sugar", SellingPrice = 10m };
        context.AddRange(location, other, register, product);
        await context.SaveChangesAsync();
        var ledger = new StockLedger(context);
        await ledger.AppendAsync([
            new StockMovementRequest(MovementType.Adjustment, product.Id, location.Id, 5m),
            new StockMovementRequest(MovementType.Adjustment, product.Id, other.Id, 9m),
        ]);
        await context.SaveChangesAsync();
        var handler = new GetSyncSnapshotQueryHandler(context);

        var first = await handler.Handle(new GetSyncSnapshotQuery(register.Id), CancellationToken.None);
        first.Products.Should().ContainSingle(p => p.Id == product.Id);
        first.Stock.Should().ContainSingle(s => s.QtyOnHand == 5m);

        var unchanged = await handler.Handle(new GetSyncSnapshotQuery(register.Id, first.Watermark), CancellationToken.None);
        unchanged.Products.Should().BeEmpty();
        unchanged.Stock.Should().BeEmpty();

        product.SellingPrice = 12m;
        await context.SaveChangesAsync();
        var delta = await handler.Handle(new GetSyncSnapshotQuery(register.Id, first.Watermark), CancellationToken.None);
        delta.Products.Should().ContainSingle(p => p.SellingPrice == 12m);
    }

    public void Dispose() => _db.Dispose();
}
