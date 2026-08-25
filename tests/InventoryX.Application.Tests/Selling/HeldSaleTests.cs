using FluentAssertions;
using InventoryX.Application.Commands.RequestHandlers.Selling;
using InventoryX.Application.Commands.Requests.Selling;
using InventoryX.Application.Services;
using InventoryX.Application.Services.IServices;
using InventoryX.Common.Tests;
using InventoryX.Domain.Models.Catalog;
using InventoryX.Domain.Models.Inventory;
using InventoryX.Domain.Models.Selling;
using InventoryX.Infrastructure.Data;
using InventoryX.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace InventoryX.Application.Tests.Selling;

public sealed class HeldSaleTests : IDisposable
{
    private readonly TestDb _db = new(Guid.NewGuid(), "cashier-1");
    private readonly Mock<IPlanEnforcer> _planEnforcer = new();

    [Fact]
    public async Task Held_sale_only_decrements_stock_when_completed()
    {
        await using var context = _db.CreateContext();
        TestPosAccess.Cashier(context, _db.TenantContext);
        var location = new Location { Name = "Main" };
        var product = new Product { Name = "Sugar", SellingPrice = 10m };
        var register = new Register { Name = "R1", LocationId = location.Id };
        var shift = new Shift { RegisterId = register.Id, OpenedBy = "cashier-1", OpenedAt = DateTime.UtcNow };
        context.Locations.Add(location);
        context.Products.Add(product);
        context.Registers.Add(register);
        context.Shifts.Add(shift);
        await context.SaveChangesAsync();
        var ledger = new StockLedger(context);
        await ledger.AppendAsync([new StockMovementRequest(MovementType.Adjustment, product.Id, location.Id, 10m)]);
        await context.SaveChangesAsync();

        var create = new CreateSaleCommandHandler(
            context, ledger, new TaxCalculator(), _db.TenantContext, _planEnforcer.Object,
            new PosAccess(context, _db.TenantContext));
        var held = await create.Handle(new CreateSaleCommand
        {
            RegisterId = register.Id,
            ShiftId = shift.Id,
            Status = "Held",
            Lines = [new CreateSaleLineDto { ProductId = product.Id, Qty = 2m }],
        }, CancellationToken.None);

        (await context.StockLevels.SingleAsync()).QtyOnHand.Should().Be(10m);

        var complete = new CompleteHeldSaleCommandHandler(context, ledger, _planEnforcer.Object, new PosAccess(context, _db.TenantContext));
        var completed = await complete.Handle(new CompleteHeldSaleCommand
        {
            SaleId = held.Id,
            Payments = [new CreateSalePaymentDto { Tender = "Cash", Amount = 20m }],
        }, CancellationToken.None);

        completed.Status.Should().Be("Completed");
        (await context.StockLevels.SingleAsync()).QtyOnHand.Should().Be(8m);
    }

    public void Dispose() => _db.Dispose();
}
