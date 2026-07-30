using FluentAssertions;
using InventoryX.Application.Queries.RequestHandlers.Inventory;
using InventoryX.Application.Queries.Requests.Inventory;
using InventoryX.Common.Tests;
using InventoryX.Domain.Models.Catalog;
using InventoryX.Domain.Models.Inventory;
using InventoryX.Infrastructure.Services;

namespace InventoryX.Application.Tests.Inventory;

public sealed class StockQueryTests : IDisposable
{
    private readonly TestDb _db = new(Guid.NewGuid(), "stock-user");

    [Fact]
    public async Task Product_rollup_and_filtered_movement_ledger_return_expected_rows()
    {
        await using var context = _db.CreateContext();
        var a = new Location { Name = "A" };
        var b = new Location { Name = "B" };
        var product = new Product { Name = "Sugar", SellingPrice = 10m };
        context.AddRange(a, b, product);
        await context.SaveChangesAsync();
        var ledger = new StockLedger(context);
        await ledger.AppendAsync([
            new StockMovementRequest(MovementType.Adjustment, product.Id, a.Id, 4m, ReasonCode: "Opening"),
            new StockMovementRequest(MovementType.Adjustment, product.Id, b.Id, 6m, ReasonCode: "Opening"),
        ]);
        await context.SaveChangesAsync();

        var stock = await new GetStockQueryHandler(context).Handle(
            new GetStockQuery { GroupBy = "product" }, CancellationToken.None);
        stock.Items.Single().QtyOnHand.Should().Be(10m);

        var movements = await new GetStockMovementsQueryHandler(context).Handle(
            new GetStockMovementsQuery { ProductId = product.Id, LocationId = a.Id, Type = "Adjustment" }, CancellationToken.None);
        movements.TotalCount.Should().Be(1);
        movements.Items.Single().LocationId.Should().Be(a.Id);
    }

    public void Dispose() => _db.Dispose();
}
