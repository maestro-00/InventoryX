using FluentAssertions;
using InventoryX.Application.Queries.RequestHandlers.Inventory;
using InventoryX.Application.Queries.Requests.Inventory;
using InventoryX.Common.Tests;
using InventoryX.Domain.Models.Inventory;
using InventoryX.Domain.Models.Purchasing;
using InventoryX.Domain.Models.Selling;

namespace InventoryX.Application.Tests.Inventory;

public sealed class BatchTraceTests : IDisposable
{
    private readonly TestDb _db = new(Guid.NewGuid());

    [Fact]
    public async Task Trace_links_batch_back_to_supplier_receipt_and_forward_to_sales()
    {
        await using var context = _db.CreateContext();
        var supplier = new Supplier { Name = "Acme" };
        var batch = new Batch { ProductId = Guid.NewGuid(), SupplierId = supplier.Id, BatchNumber = "LOT-1" };
        var receipt = new GoodsReceipt
        {
            PurchaseOrderId = Guid.NewGuid(), SupplierId = supplier.Id, LocationId = Guid.NewGuid(),
            ReceiptNumber = "GR-1", ReceivedBy = "receiver",
            Lines = [new GoodsReceiptLine { PurchaseOrderLineId = Guid.NewGuid(), ProductId = batch.ProductId, BatchId = batch.Id, QtyReceived = 10m }],
        };
        var sale = new Sale
        {
            LocationId = receipt.LocationId, RegisterId = Guid.NewGuid(), ShiftId = Guid.NewGuid(), CashierId = "cashier",
            ClientSaleId = Guid.NewGuid(), OccurredAt = DateTime.UtcNow,
            Lines = [new SaleLine { ProductId = batch.ProductId, BatchId = batch.Id, ProductName = "Milk", Qty = 2m }],
        };
        context.AddRange(supplier, batch, receipt, sale);
        await context.SaveChangesAsync();

        var result = await new GetBatchTraceQueryHandler(context).Handle(new GetBatchTraceQuery(batch.Id), CancellationToken.None);

        result.Supplier.Should().NotBeNull();
        result.Supplier!.Id.Should().Be(supplier.Id);
        result.Receipts.Should().ContainSingle(item => item.Id == receipt.Id && item.Quantity == 10m);
        result.Sales.Should().ContainSingle(item => item.Id == sale.Id && item.Quantity == 2m && item.CashierId == "cashier");
    }

    public void Dispose() => _db.Dispose();
}
