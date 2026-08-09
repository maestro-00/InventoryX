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
using Moq;

namespace InventoryX.Application.Tests.Selling;

/// <summary>T045 - split tenders, cash change, and tender-sum validation.</summary>
public sealed class SalePaymentTests : IDisposable
{
    private readonly TestDb _db = new(Guid.NewGuid(), "cashier-1");
    private readonly Mock<IPlanEnforcer> _planEnforcer = new();

    private async Task<(CreateSaleCommandHandler Handler, Guid ProductId, Guid RegisterId, Guid ShiftId)> SetupAsync()
    {
        var context = _db.CreateContext();
        var location = new Location { Name = "Main Shop" };
        var product = new Product { Name = "Sugar", SellingPrice = 10m };
        var register = new Register { Name = "R1", LocationId = location.Id };
        var shift = new Shift
        {
            RegisterId = register.Id,
            OpenedBy = "cashier-1",
            OpenedAt = DateTime.UtcNow,
            OpeningFloat = 100m,
        };
        context.Locations.Add(location);
        context.Products.Add(product);
        context.Registers.Add(register);
        context.Shifts.Add(shift);
        await context.SaveChangesAsync();

        var ledger = new StockLedger(context);
        await ledger.AppendAsync([
            new StockMovementRequest(MovementType.Adjustment, product.Id, location.Id, 10m),
        ]);
        await context.SaveChangesAsync();

        return (
            new CreateSaleCommandHandler(
                context,
                ledger,
                new TaxCalculator(),
                _db.TenantContext,
                _planEnforcer.Object),
            product.Id,
            register.Id,
            shift.Id);
    }

    private static CreateSaleCommand Sale(
        Guid productId,
        Guid registerId,
        Guid shiftId,
        params CreateSalePaymentDto[] payments) => new()
    {
        RegisterId = registerId,
        ShiftId = shiftId,
        Lines = [new CreateSaleLineDto { ProductId = productId, Qty = 2m }],
        Payments = payments.ToList(),
    };

    [Fact]
    public async Task Split_cash_and_card_tenders_are_recorded()
    {
        var (handler, productId, registerId, shiftId) = await SetupAsync();

        var result = await handler.Handle(Sale(
            productId,
            registerId,
            shiftId,
            new CreateSalePaymentDto { Tender = "Cash", Amount = 5m },
            new CreateSalePaymentDto { Tender = "Card", Amount = 15m }),
            CancellationToken.None);

        result.Payments.Should().HaveCount(2);
        result.Payments.Should().Contain(p => p.Tender == "Cash" && p.Amount == 5m);
        result.Payments.Should().Contain(p => p.Tender == "Card" && p.Amount == 15m);
        result.ChangeDue.Should().Be(0m);
    }

    [Fact]
    public async Task Cash_overpayment_returns_change()
    {
        var (handler, productId, registerId, shiftId) = await SetupAsync();

        var result = await handler.Handle(Sale(
            productId,
            registerId,
            shiftId,
            new CreateSalePaymentDto { Tender = "Cash", Amount = 25m }),
            CancellationToken.None);

        result.ChangeDue.Should().Be(5m);
    }

    [Fact]
    public async Task Tender_sum_below_total_is_rejected()
    {
        var (handler, productId, registerId, shiftId) = await SetupAsync();

        var act = () => handler.Handle(Sale(
            productId,
            registerId,
            shiftId,
            new CreateSalePaymentDto { Tender = "Cash", Amount = 19.99m }),
            CancellationToken.None);

        await act.Should().ThrowAsync<FluentValidation.ValidationException>();
    }

    [Fact]
    public async Task Negative_tender_row_is_rejected_even_when_sum_covers_total()
    {
        var (handler, productId, registerId, shiftId) = await SetupAsync();

        var act = () => handler.Handle(Sale(
            productId,
            registerId,
            shiftId,
            new CreateSalePaymentDto { Tender = "Cash", Amount = 25m },
            new CreateSalePaymentDto { Tender = "Card", Amount = -5m }),
            CancellationToken.None);

        await act.Should().ThrowAsync<FluentValidation.ValidationException>();
    }

    public void Dispose() => _db.Dispose();
}
