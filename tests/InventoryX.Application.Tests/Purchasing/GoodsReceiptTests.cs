using FluentAssertions;
using InventoryX.Application.Commands.RequestHandlers.Purchasing;
using InventoryX.Application.Commands.Requests.Purchasing;
using InventoryX.Common.Tests;
using InventoryX.Domain.Models.Catalog;
using InventoryX.Domain.Models.Inventory;
using InventoryX.Domain.Models.Purchasing;
using InventoryX.Domain.Models.Tenancy;
using InventoryX.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace InventoryX.Application.Tests.Purchasing;

public sealed class GoodsReceiptTests : IDisposable
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly TestDb _db;
    public GoodsReceiptTests() => _db = new TestDb(_tenantId, "receiver-1");

    [Fact]
    public async Task Short_damaged_batch_receipt_posts_only_accepted_stock_and_keeps_order_partial()
    {
        await using var context = _db.CreateContext();
        var (order, line, product, location) = await SeedAsync(context, requireExpiry: true);

        var result = await new RecordGoodsReceiptCommandHandler(context, new StockLedger(context), _db.TenantContext)
            .Handle(new RecordGoodsReceiptCommand
            {
                PurchaseOrderId = order.Id, LocationId = location.Id,
                Lines = [new GoodsReceiptLineInput(line.Id, 80m, 5m, 12m, "LOT-001", DateTime.UtcNow.Date.AddMonths(6))],
            }, CancellationToken.None);

        result.PurchaseOrderStatus.Should().Be(PurchaseOrderStatus.PartiallyReceived);
        result.Lines.Single().AcceptedQty.Should().Be(75m);
        (await context.StockLevels.SingleAsync()).QtyOnHand.Should().Be(75m);
        var movement = await context.StockMovements.SingleAsync();
        movement.Type.Should().Be(MovementType.Receipt);
        movement.BatchId.Should().NotBeNull();
        (await context.Batches.SingleAsync()).BatchNumber.Should().Be("LOT-001");
    }

    [Fact]
    public async Task Later_receipt_of_balance_moves_order_to_fully_received()
    {
        await using var context = _db.CreateContext();
        var (order, line, _, location) = await SeedAsync(context, requireExpiry: false);
        var handler = new RecordGoodsReceiptCommandHandler(context, new StockLedger(context), _db.TenantContext);
        await handler.Handle(new RecordGoodsReceiptCommand
        {
            PurchaseOrderId = order.Id, LocationId = location.Id,
            Lines = [new GoodsReceiptLineInput(line.Id, 80m, 5m, 12m, "LOT-001", null)],
        }, CancellationToken.None);

        var result = await handler.Handle(new RecordGoodsReceiptCommand
        {
            PurchaseOrderId = order.Id, LocationId = location.Id,
            Lines = [new GoodsReceiptLineInput(line.Id, 25m, 0m, 12m, "LOT-002", null)],
        }, CancellationToken.None);

        result.PurchaseOrderStatus.Should().Be(PurchaseOrderStatus.FullyReceived);
        (await context.StockLevels.SumAsync(level => level.QtyOnHand)).Should().Be(100m);
    }

    [Fact]
    public async Task Expiry_is_required_for_batch_product_when_tenant_setting_is_enabled()
    {
        await using var context = _db.CreateContext();
        var (order, line, _, location) = await SeedAsync(context, requireExpiry: true);
        var handler = new RecordGoodsReceiptCommandHandler(context, new StockLedger(context), _db.TenantContext);

        var act = () => handler.Handle(new RecordGoodsReceiptCommand
        {
            PurchaseOrderId = order.Id, LocationId = location.Id,
            Lines = [new GoodsReceiptLineInput(line.Id, 10m, 0m, 12m, "LOT-001", null)],
        }, CancellationToken.None);

        await act.Should().ThrowAsync<FluentValidation.ValidationException>();
    }

    [Fact]
    public async Task Partial_order_can_close_short_only_with_a_reason()
    {
        await using var context = _db.CreateContext();
        var (order, line, _, location) = await SeedAsync(context, requireExpiry: false);
        await new RecordGoodsReceiptCommandHandler(context, new StockLedger(context), _db.TenantContext)
            .Handle(new RecordGoodsReceiptCommand
            {
                PurchaseOrderId = order.Id, LocationId = location.Id,
                Lines = [new GoodsReceiptLineInput(line.Id, 80m, 0m, 12m, "LOT-001", null)],
            }, CancellationToken.None);

        var closed = await new ClosePurchaseOrderShortCommandHandler(context).Handle(
            new ClosePurchaseOrderShortCommand(order.Id, "Supplier cannot fulfil balance"), CancellationToken.None);

        closed.Status.Should().Be(PurchaseOrderStatus.Closed);
        (await context.PurchaseOrders.SingleAsync()).ClosedShortReason.Should().Be("Supplier cannot fulfil balance");
    }

    private async Task<(PurchaseOrder Order, PurchaseOrderLine Line, Product Product, Location Location)> SeedAsync(
        Infrastructure.Data.AppDbContext context, bool requireExpiry)
    {
        context.Tenants.Add(new Tenant { Id = _tenantId, Name = "Shop", RequireExpiryOnBatchReceipt = requireExpiry });
        var supplier = new Supplier { Name = "Acme" };
        var location = new Location { Name = "Warehouse" };
        var product = new Product { Name = "Milk", TrackingMode = TrackingMode.Batch, SellingPrice = 15m };
        var order = new PurchaseOrder
        {
            Supplier = supplier, SupplierId = supplier.Id, DeliverToLocationId = location.Id,
            Lines = [new PurchaseOrderLine { ProductId = product.Id, Description = product.Name, OrderedQty = 100m, UnitCost = 12m }],
        };
        order.Submit(false, DateTime.UtcNow);
        context.AddRange(location, product, order);
        await context.SaveChangesAsync();
        return (order, order.Lines.Single(), product, location);
    }

    public void Dispose() => _db.Dispose();
}
