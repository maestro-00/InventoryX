using FluentAssertions;
using InventoryX.Application.Commands.RequestHandlers.Purchasing;
using InventoryX.Application.Commands.Requests.Purchasing;
using InventoryX.Application.Exceptions;
using InventoryX.Application.Queries.RequestHandlers.Inventory;
using InventoryX.Application.Queries.RequestHandlers.Purchasing;
using InventoryX.Application.Queries.Requests.Inventory;
using InventoryX.Application.Queries.Requests.Purchasing;
using InventoryX.Common.Tests;
using InventoryX.Domain.Models.Catalog;
using InventoryX.Domain.Models.Inventory;

namespace InventoryX.Application.Tests.Purchasing;

public sealed class SupplierAndTransferQueryTests : IDisposable
{
    private readonly TestDb _db = new(Guid.NewGuid());

    [Fact]
    public async Task Pages_suppliers_and_updates_with_etag_guard()
    {
        await using var context = _db.CreateContext();
        var create = new CreateSupplierCommandHandler(context);
        await create.Handle(new CreateSupplierCommand("Acme", "a@acme.test", null, LeadTimeDays: 5), CancellationToken.None);
        await create.Handle(new CreateSupplierCommand("Beta", null, null), CancellationToken.None);

        var page = await new GetSuppliersQueryHandler(context).Handle(
            new GetSuppliersQuery { Page = 1, PageSize = 1 }, CancellationToken.None);
        page.Items.Should().HaveCount(1);
        page.TotalCount.Should().Be(2);

        var supplier = page.Items[0];
        var updated = await new UpdateSupplierCommandHandler(context).Handle(
            new UpdateSupplierCommand { Id = supplier.Id, Name = "Acme Co" }, CancellationToken.None);
        updated.Name.Should().Be("Acme Co");

        var stale = () => new UpdateSupplierCommandHandler(context).Handle(
            new UpdateSupplierCommand { Id = supplier.Id, Name = "X", ExpectedRowVersion = [9] }, CancellationToken.None);
        await stale.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Lists_transfers_filtered_by_status()
    {
        await using var context = _db.CreateContext();
        var source = new Location { Name = "A" };
        var dest = new Location { Name = "B" };
        var product = new Product { Name = "Widget", SellingPrice = 1m, CostPrice = 0.5m };
        context.Locations.AddRange(source, dest);
        context.Products.Add(product);
        await context.SaveChangesAsync();

        await new InventoryX.Application.Commands.RequestHandlers.Inventory.CreateStockTransferCommandHandler(context)
            .Handle(new InventoryX.Application.Commands.Requests.Inventory.CreateStockTransferCommand
            {
                FromLocationId = source.Id,
                ToLocationId = dest.Id,
                Lines = [new InventoryX.Application.Commands.Requests.Inventory.TransferLineInput(product.Id, 1m)],
            }, CancellationToken.None);

        var result = await new GetTransfersQueryHandler(context).Handle(
            new GetTransfersQuery { Status = StockTransferStatus.Draft }, CancellationToken.None);
        result.TotalCount.Should().Be(1);
        result.Items[0].Status.Should().Be(nameof(StockTransferStatus.Draft));
    }

    public void Dispose() => _db.Dispose();
}
