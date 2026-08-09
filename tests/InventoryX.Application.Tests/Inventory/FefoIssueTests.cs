using FluentAssertions;
using InventoryX.Application.Commands.RequestHandlers.Selling;
using InventoryX.Application.Commands.Requests.Selling;
using InventoryX.Application.Services;
using InventoryX.Application.Services.IServices;
using InventoryX.Common.Tests;
using InventoryX.Domain.Models.Catalog;
using InventoryX.Domain.Models.Inventory;
using InventoryX.Domain.Models.Selling;
using InventoryX.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace InventoryX.Application.Tests.Inventory;

public sealed class FefoIssueTests : IDisposable
{
    private readonly TestDb _db = new(Guid.NewGuid(), "cashier-1");

    [Fact]
    public async Task Sale_spanning_batches_issues_earliest_expiry_first_and_splits_attribution()
    {
        await using var context = _db.CreateContext();
        var setup = await SeedAsync(context);
        var handler = CreateHandler(context);

        var result = await handler.Handle(NewSale(setup, 3m), CancellationToken.None);

        result.Lines.Should().HaveCount(2);
        result.Lines[0].BatchId.Should().Be(setup.EarlyBatchId);
        result.Lines[0].Qty.Should().Be(2m);
        result.Lines[1].BatchId.Should().Be(setup.LaterBatchId);
        result.Lines[1].Qty.Should().Be(1m);
        (await context.StockLevels.SingleAsync(level => level.BatchId == setup.EarlyBatchId)).QtyOnHand.Should().Be(0m);
        (await context.StockLevels.SingleAsync(level => level.BatchId == setup.LaterBatchId)).QtyOnHand.Should().Be(4m);
    }

    [Fact]
    public async Task Explicit_batch_override_issues_requested_lot_even_when_it_expires_later()
    {
        await using var context = _db.CreateContext();
        var setup = await SeedAsync(context);

        var result = await CreateHandler(context).Handle(NewSale(setup, 1m, setup.LaterBatchId), CancellationToken.None);

        result.Lines.Should().ContainSingle().Which.BatchId.Should().Be(setup.LaterBatchId);
        (await context.StockLevels.SingleAsync(level => level.BatchId == setup.EarlyBatchId)).QtyOnHand.Should().Be(2m);
        (await context.StockLevels.SingleAsync(level => level.BatchId == setup.LaterBatchId)).QtyOnHand.Should().Be(4m);
    }

    private CreateSaleCommandHandler CreateHandler(Infrastructure.Data.AppDbContext context) => new(
        context, new StockLedger(context), new TaxCalculator(), _db.TenantContext, Mock.Of<IPlanEnforcer>());

    private static CreateSaleCommand NewSale(Setup setup, decimal qty, Guid? batchId = null) => new()
    {
        RegisterId = setup.RegisterId, ShiftId = setup.ShiftId,
        Lines = [new CreateSaleLineDto { ProductId = setup.ProductId, Qty = qty, BatchId = batchId }],
        Payments = [new CreateSalePaymentDto { Tender = "Cash", Amount = 100m }],
    };

    private static async Task<Setup> SeedAsync(Infrastructure.Data.AppDbContext context)
    {
        var location = new Location { Name = "Shop" };
        var product = new Product { Name = "Milk", TrackingMode = TrackingMode.Batch, SellingPrice = 10m };
        var register = new Register { Name = "R1", LocationId = location.Id };
        var shift = new Shift { RegisterId = register.Id, OpenedBy = "cashier-1", OpenedAt = DateTime.UtcNow };
        var early = new Batch { ProductId = product.Id, BatchNumber = "EARLY", ExpiresAt = DateTime.UtcNow.AddDays(10) };
        var later = new Batch { ProductId = product.Id, BatchNumber = "LATER", ExpiresAt = DateTime.UtcNow.AddDays(30) };
        context.AddRange(location, product, register, shift, early, later);
        await context.SaveChangesAsync();
        var ledger = new StockLedger(context);
        await ledger.AppendAsync([
            new StockMovementRequest(MovementType.Receipt, product.Id, location.Id, 2m, BatchId: early.Id, UnitCost: 5m),
            new StockMovementRequest(MovementType.Receipt, product.Id, location.Id, 5m, BatchId: later.Id, UnitCost: 5m),
        ]);
        await context.SaveChangesAsync();
        return new Setup(product.Id, register.Id, shift.Id, early.Id, later.Id);
    }

    private sealed record Setup(Guid ProductId, Guid RegisterId, Guid ShiftId, Guid EarlyBatchId, Guid LaterBatchId);
    public void Dispose() => _db.Dispose();
}
