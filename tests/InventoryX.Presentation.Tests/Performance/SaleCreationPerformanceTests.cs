using System.Diagnostics;
using InventoryX.Application.Commands.RequestHandlers.Selling;
using InventoryX.Application.Commands.Requests.Selling;
using InventoryX.Application.Services;
using InventoryX.Application.Services.IServices;
using InventoryX.Common.Tests;
using InventoryX.Domain.Models.Catalog;
using InventoryX.Domain.Models.Inventory;
using InventoryX.Domain.Models.Selling;
using InventoryX.Infrastructure.Services;
using Moq;

namespace InventoryX.Presentation.Tests.Performance;

public sealed class SaleCreationPerformanceTests
{
    private const int ProductCount = 100_000;
    private const int MeasuredSales = 20;

    [Fact]
    public async Task Sale_creation_p95_is_below_300ms_with_100k_product_tenant()
    {
        var tenantId = Guid.NewGuid();
        using var database = new TestDb(tenantId, "performance-cashier");
        await using var context = database.CreateContext();
        var tax = new TaxTreatment { Code = "PERF", Name = "Performance tax", ComponentsJson = "[]" };
        var location = new Location { Name = "Performance Shop" };
        var target = new Product { Name = "Target product", Sku = "TARGET-001", SellingPrice = 10m, CostPrice = 5m, TaxTreatment = tax };
        var register = new Register { Name = "Performance Register", LocationId = location.Id };
        var shift = new Shift { RegisterId = register.Id, OpenedBy = "performance-cashier", OpenedAt = DateTime.UtcNow, OpeningFloat = 100m };
        context.AddRange(tax, location, target, register, shift);
        context.Products.AddRange(Enumerable.Range(0, ProductCount - 1).Select(index => new Product
        {
            Name = $"Seeded product {index:D6}",
            Sku = $"SKU-{index:D6}",
            SellingPrice = 10m,
            CostPrice = 5m,
            TaxTreatment = tax,
        }));
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var ledger = new StockLedger(context);
        await ledger.AppendAsync([new StockMovementRequest(MovementType.Adjustment, target.Id, location.Id, 100m, UnitCost: 5m, ReasonCode: "Performance seed")]);
        await context.SaveChangesAsync();
        TestPosAccess.Cashier(context, database.TenantContext);
        var handler = new CreateSaleCommandHandler(context, ledger, new TaxCalculator(), database.TenantContext, new Mock<IPlanEnforcer>().Object, new PosAccess(context, database.TenantContext));

        await handler.Handle(Command(target.Id, register.Id, shift.Id), CancellationToken.None);
        var elapsed = new List<double>(MeasuredSales);
        for (var index = 0; index < MeasuredSales; index++)
        {
            var stopwatch = Stopwatch.StartNew();
            await handler.Handle(Command(target.Id, register.Id, shift.Id), CancellationToken.None);
            stopwatch.Stop();
            elapsed.Add(stopwatch.Elapsed.TotalMilliseconds);
        }

        var p95 = elapsed.OrderBy(value => value).ElementAt((int)Math.Ceiling(elapsed.Count * 0.95) - 1);
        Assert.True(p95 < 300, $"Sale creation p95 was {p95:F1} ms; samples: {string.Join(", ", elapsed.Select(value => value.ToString("F1")))}");
    }

    private static CreateSaleCommand Command(Guid productId, Guid registerId, Guid shiftId) => new()
    {
        ClientSaleId = Guid.NewGuid(), RegisterId = registerId, ShiftId = shiftId,
        Lines = [new CreateSaleLineDto { ProductId = productId, Qty = 1m }],
        Payments = [new CreateSalePaymentDto { Tender = "Cash", Amount = 10m }],
    };
}
