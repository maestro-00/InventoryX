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

namespace InventoryX.Application.Tests.Sync;

public sealed class StockConflictTests : IDisposable
{
    private readonly TestDb _db = new(Guid.NewGuid(), "cashier-1");

    [Fact]
    public async Task Offline_sale_that_drives_stock_negative_is_recorded_and_flagged()
    {
        await using var context = _db.CreateContext();
        var location = new Location { Name = "Main" };
        var product = new Product { Name = "Sugar", SellingPrice = 10m };
        var register = new Register { Name = "R1", LocationId = location.Id };
        var shift = new Shift { RegisterId = register.Id, OpenedBy = "cashier-1", OpenedAt = DateTime.UtcNow };
        context.AddRange(location, product, register, shift);
        await context.SaveChangesAsync();
        var ledger = new StockLedger(context);
        await ledger.AppendAsync([new StockMovementRequest(MovementType.Adjustment, product.Id, location.Id, 1m)]);
        await context.SaveChangesAsync();

        var handler = new CreateSaleCommandHandler(context, ledger, new TaxCalculator(), _db.TenantContext, Mock.Of<IPlanEnforcer>());
        var result = await handler.Handle(new CreateSaleCommand
        {
            RegisterId = register.Id, ShiftId = shift.Id, OfflineOrigin = true, AllowNegativeStock = true,
            Lines = [new CreateSaleLineDto { ProductId = product.Id, Qty = 2m }],
            Payments = [new CreateSalePaymentDto { Tender = "Cash", Amount = 20m }],
        }, CancellationToken.None);

        result.StockConflictFlag.Should().BeTrue();
        (await context.StockLevels.SingleAsync()).QtyOnHand.Should().Be(-1m);
        (await context.Sales.SingleAsync()).StockConflictFlag.Should().BeTrue();
        (await context.StockMovements.CountAsync(m => m.Type == MovementType.Sale)).Should().Be(1);
    }

    public void Dispose() => _db.Dispose();
}
