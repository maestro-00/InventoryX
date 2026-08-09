using FluentAssertions;
using InventoryX.Application.Commands.RequestHandlers.Inventory;
using InventoryX.Application.Commands.Requests.Inventory;
using InventoryX.Common.Tests;
using InventoryX.Domain.Models.Inventory;
using InventoryX.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace InventoryX.Application.Tests.Inventory;

public sealed class StockTransferTests : IDisposable
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _productId = Guid.NewGuid();
    private readonly Guid _sourceId = Guid.NewGuid();
    private readonly Guid _destinationId = Guid.NewGuid();
    private readonly TestDb _db;

    public StockTransferTests() => _db = new TestDb(_tenantId);

    [Fact]
    public async Task Dispatch_then_full_receive_moves_stock_and_clears_in_transit()
    {
        await using var context = _db.CreateContext();
        await SeedAsync(context);
        var create = new CreateStockTransferCommandHandler(context);
        var transfer = await create.Handle(new CreateStockTransferCommand
        {
            FromLocationId = _sourceId, ToLocationId = _destinationId,
            Lines = [new TransferLineInput(_productId, 4m)],
        }, CancellationToken.None);
        var ledger = new StockLedger(context);
        await new DispatchStockTransferCommandHandler(context, ledger)
            .Handle(new DispatchStockTransferCommand { TransferId = transfer.Id }, CancellationToken.None);
        (await context.StockLevels.SingleAsync(level => level.LocationId == _sourceId)).QtyInTransit.Should().Be(4m);
        var line = await context.StockTransferLines.SingleAsync();

        var result = await new ReceiveStockTransferCommandHandler(context, ledger).Handle(new ReceiveStockTransferCommand
        { TransferId = transfer.Id, Lines = [new ReceiveTransferLineInput(line.Id, 4m)] }, CancellationToken.None);

        result.Status.Should().Be(nameof(StockTransferStatus.Received));
        (await context.StockLevels.SingleAsync(level => level.LocationId == _sourceId)).QtyInTransit.Should().Be(0m);
        (await context.StockLevels.SingleAsync(level => level.LocationId == _destinationId)).QtyOnHand.Should().Be(4m);
    }

    [Fact]
    public async Task Partial_receive_requires_reason_and_sets_discrepancy_status()
    {
        await using var context = _db.CreateContext();
        await SeedAsync(context);
        var transfer = await new CreateStockTransferCommandHandler(context).Handle(new CreateStockTransferCommand
        { FromLocationId = _sourceId, ToLocationId = _destinationId, Lines = [new TransferLineInput(_productId, 4m)] }, CancellationToken.None);
        var ledger = new StockLedger(context);
        await new DispatchStockTransferCommandHandler(context, ledger).Handle(new DispatchStockTransferCommand { TransferId = transfer.Id }, CancellationToken.None);
        var line = await context.StockTransferLines.SingleAsync();
        var handler = new ReceiveStockTransferCommandHandler(context, ledger);

        var invalid = () => handler.Handle(new ReceiveStockTransferCommand
        { TransferId = transfer.Id, Lines = [new ReceiveTransferLineInput(line.Id, 3m)] }, CancellationToken.None);
        await invalid.Should().ThrowAsync<FluentValidation.ValidationException>();
        var result = await handler.Handle(new ReceiveStockTransferCommand
        { TransferId = transfer.Id, Lines = [new ReceiveTransferLineInput(line.Id, 3m)], DiscrepancyReason = "Damaged carton" }, CancellationToken.None);

        result.Status.Should().Be(nameof(StockTransferStatus.ReceivedWithDiscrepancy));
    }

    private async Task SeedAsync(InventoryX.Infrastructure.Data.AppDbContext context)
    {
        context.Locations.AddRange(new Location { Id = _sourceId, TenantId = _tenantId, Name = "Source" },
            new Location { Id = _destinationId, TenantId = _tenantId, Name = "Destination" });
        await context.SaveChangesAsync();
        var ledger = new StockLedger(context);
        await ledger.AppendAsync([new(MovementType.Adjustment, _productId, _sourceId, 10m, UnitCost: 2m)]);
        await context.SaveChangesAsync();
    }

    public void Dispose() => _db.Dispose();
}
