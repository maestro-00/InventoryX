using FluentAssertions;
using InventoryX.Application.Commands.RequestHandlers.Selling;
using InventoryX.Application.Commands.Requests.Selling;
using InventoryX.Application.Exceptions;
using InventoryX.Application.Services;
using InventoryX.Application.Services.IServices;
using InventoryX.Common.Tests;
using InventoryX.Domain.Models.Catalog;
using InventoryX.Domain.Models.Inventory;
using InventoryX.Domain.Models.Selling;
using InventoryX.Domain.Models.Tenancy;
using InventoryX.Infrastructure.Data;
using InventoryX.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace InventoryX.Application.Tests.Selling;

/// <summary>
/// T030 — CreateSaleCommand: stock decrement, Ghana tax snapshot math from
/// quickstart scenario A, the open-shift invariant and ClientSaleId idempotency.
/// </summary>
public sealed class CreateSaleCommandTests : IDisposable
{
    private static readonly Guid Tenant = Guid.NewGuid();
    private readonly TestDb _db = new(Tenant, "cashier-1");
    private readonly Mock<IPlanEnforcer> _planEnforcer = new();

    private const string GhStdComponents =
        """
        [{"code":"NHIL","name":"NHIL","rate":0.025,"base":"net"},
         {"code":"GETFUND","name":"GETFund","rate":0.025,"base":"net"},
         {"code":"COVID","name":"COVID levy","rate":0.01,"base":"net"},
         {"code":"VAT","name":"VAT","rate":0.15,"base":"net_plus_levies"}]
        """;

    private async Task<(AppDbContext Context, CreateSaleCommandHandler Handler, Guid ProductId, Guid RegisterId, Guid ShiftId, Guid LocationId)> SetupAsync(
        bool openShift = true, decimal openingStock = 10m)
    {
        var context = _db.CreateContext();
        var tax = new TaxTreatment { Code = "GH-STD", Name = "Ghana Standard", ComponentsJson = GhStdComponents };
        context.TaxTreatments.Add(tax);
        _db.TenantContext.Role = "Cashier";
        context.AppRoles.Add(new Role
        {
            Name = "Cashier",
            Permissions = Permission.Sell,
            IsSystem = true,
        });
        var location = new Location { Name = "Main Shop" };
        var product = new Product { Name = "Sugar 1kg", SellingPrice = 10m, CostPrice = 6m, TaxTreatment = tax };
        context.Locations.Add(location);
        context.Products.Add(product);
        var register = new Register { Name = "R1", LocationId = location.Id };
        context.Registers.Add(register);
        Shift? shift = null;
        if (openShift)
        {
            shift = new Shift { RegisterId = register.Id, OpenedBy = "cashier-1", OpenedAt = DateTime.UtcNow, OpeningFloat = 100m };
            context.Shifts.Add(shift);
        }
        await context.SaveChangesAsync();

        if (openingStock > 0)
        {
            var ledger = new StockLedger(context);
            await ledger.AppendAsync([
                new StockMovementRequest(MovementType.Adjustment, product.Id, location.Id, openingStock, UnitCost: 6m, ReasonCode: "Correction"),
            ]);
            await context.SaveChangesAsync();
        }

        var handler = new CreateSaleCommandHandler(
            context, new StockLedger(context), new TaxCalculator(), _db.TenantContext, _planEnforcer.Object,
            new PosAccess(context, _db.TenantContext));
        return (context, handler, product.Id, register.Id, shift?.Id ?? Guid.Empty, location.Id);
    }

    private static CreateSaleCommand NewSale(Guid productId, Guid registerId, Guid shiftId, decimal qty = 2m, decimal cash = 25m) => new()
    {
        ClientSaleId = Guid.NewGuid(),
        RegisterId = registerId,
        ShiftId = shiftId,
        Lines = [new CreateSaleLineDto { ProductId = productId, Qty = qty }],
        Payments = [new CreateSalePaymentDto { Tender = "Cash", Amount = cash }],
    };

    [Fact]
    public async Task Sale_computes_ghana_tax_snapshot_and_change_due()
    {
        var (context, handler, productId, registerId, shiftId, _) = await SetupAsync();

        var result = await handler.Handle(NewSale(productId, registerId, shiftId), CancellationToken.None);

        result.Subtotal.Should().Be(20.00m);
        result.TaxTotal.Should().Be(4.38m);
        result.GrandTotal.Should().Be(24.38m);
        result.ChangeDue.Should().Be(0.62m);

        var line = await context.SaleLines.SingleAsync();
        line.TaxComponents.Should().ContainAll("NHIL", "GETFUND", "COVID", "VAT");
        await context.DisposeAsync();
    }

    [Fact]
    public async Task Sale_decrements_stock_from_10_to_8()
    {
        var (context, handler, productId, registerId, shiftId, locationId) = await SetupAsync();

        await handler.Handle(NewSale(productId, registerId, shiftId), CancellationToken.None);

        var level = await context.StockLevels.SingleAsync(l => l.ProductId == productId && l.LocationId == locationId);
        level.QtyOnHand.Should().Be(8m);
        (await context.StockMovements.CountAsync(m => m.Type == MovementType.Sale)).Should().Be(1);
        await context.DisposeAsync();
    }

    [Fact]
    public async Task Sale_without_open_shift_is_rejected()
    {
        var (context, handler, productId, registerId, _, _) = await SetupAsync(openShift: false);

        var act = () => handler.Handle(NewSale(productId, registerId, Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>().WithMessage("*shift*");
        await context.DisposeAsync();
    }

    [Fact]
    public async Task Replaying_same_client_sale_id_returns_original_without_double_decrement()
    {
        var (context, handler, productId, registerId, shiftId, locationId) = await SetupAsync();
        var command = NewSale(productId, registerId, shiftId);

        var first = await handler.Handle(command, CancellationToken.None);
        var replay = await handler.Handle(command, CancellationToken.None);

        replay.Id.Should().Be(first.Id);
        (await context.StockLevels.SingleAsync(l => l.LocationId == locationId)).QtyOnHand.Should().Be(8m);
        (await context.Sales.CountAsync()).Should().Be(1);
        await context.DisposeAsync();
    }

    [Fact]
    public async Task Insufficient_payment_for_completed_sale_is_rejected()
    {
        var (context, handler, productId, registerId, shiftId, _) = await SetupAsync();

        var act = () => handler.Handle(NewSale(productId, registerId, shiftId, cash: 5m), CancellationToken.None);

        await act.Should().ThrowAsync<FluentValidation.ValidationException>();
        await context.DisposeAsync();
    }

    public void Dispose() => _db.Dispose();
}
