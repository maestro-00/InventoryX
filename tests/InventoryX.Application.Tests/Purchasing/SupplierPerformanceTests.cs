using FluentAssertions;
using InventoryX.Application.Queries.RequestHandlers.Purchasing;
using InventoryX.Application.Queries.Requests.Purchasing;
using InventoryX.Common.Tests;
using InventoryX.Domain.Models.Purchasing;
using InventoryX.Infrastructure.Data;

namespace InventoryX.Application.Tests.Purchasing;

public sealed class SupplierPerformanceTests : IDisposable
{
    private readonly TestDb _db = new(Guid.NewGuid());

    [Fact]
    public async Task Performance_reports_on_time_rate_lead_time_and_received_price_history()
    {
        await using var context = _db.CreateContext();
        var supplier = new Supplier { Name = "Acme" };
        var product = new InventoryX.Domain.Models.Catalog.Product { Name = "Sugar" };
        var sentAt = DateTime.UtcNow.AddDays(-10);
        var order = new PurchaseOrder
        {
            Supplier = supplier, SupplierId = supplier.Id, RequiredBy = DateTime.UtcNow.AddDays(-2),
            Lines = [new PurchaseOrderLine { ProductId = product.Id, Description = "Sugar", OrderedQty = 10m, UnitCost = 8m }],
        };
        order.Submit(false, sentAt);
        var receipt = new GoodsReceipt
        {
            PurchaseOrderId = order.Id, SupplierId = supplier.Id, LocationId = Guid.NewGuid(), ReceiptNumber = "GR-1",
            ReceivedAt = DateTime.UtcNow.AddDays(-3),
            Lines = [new GoodsReceiptLine { PurchaseOrderLineId = order.Lines[0].Id, ProductId = product.Id, QtyReceived = 10m, UnitCost = 9m }],
        };
        context.AddRange(supplier, product, order, receipt);
        await context.SaveChangesAsync();

        var result = await new SupplierPerformanceQueryHandler(context).Handle(new GetSupplierPerformanceQuery(supplier.Id), CancellationToken.None);

        result.TotalOrders.Should().Be(1);
        result.OnTimeOrders.Should().Be(1);
        result.OnTimeRatePercent.Should().Be(100);
        result.AverageLeadTimeDays.Should().BeApproximately(7, 0.2);
        result.PriceHistory.Should().ContainSingle().Which.UnitCost.Should().Be(9m);
    }

    public void Dispose() => _db.Dispose();
}
