using FluentAssertions;
using InventoryX.Application.Behaviors;
using InventoryX.Application.Commands.RequestHandlers.Selling;
using InventoryX.Application.Commands.Requests.Selling;
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

/// <summary>T047 - role discount caps, escalation attribution, and audit coverage.</summary>
public sealed class DiscountPolicyTests : IDisposable
{
    private readonly TestDb _db = new(Guid.NewGuid(), "cashier-1");
    private readonly Mock<IPlanEnforcer> _planEnforcer = new();

    private async Task<(AppDbContext Context, CreateSaleCommandHandler Handler, Guid ProductId, Guid RegisterId, Guid ShiftId)> SetupAsync()
    {
        _db.TenantContext.Role = "Cashier";
        var context = _db.CreateContext();
        context.AppRoles.Add(new Role
        {
            Name = "Cashier",
            Permissions = Permission.Sell | Permission.Discount,
            MaxDiscountPercent = 5m,
            IsSystem = true,
        });
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
            context,
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
        decimal discount,
        string? authorizedBy = null) => new()
    {
        RegisterId = registerId,
        ShiftId = shiftId,
        Lines =
        [
            new CreateSaleLineDto
            {
                ProductId = productId,
                Qty = 2m,
                LineDiscount = discount,
                DiscountAuthorizedBy = authorizedBy,
            },
        ],
        Payments = [new CreateSalePaymentDto { Tender = "Cash", Amount = 20m }],
    };

    [Fact]
    public async Task Discount_above_role_cap_requires_manager_escalation()
    {
        var (_, handler, productId, registerId, shiftId) = await SetupAsync();

        var act = () => handler.Handle(
            Sale(productId, registerId, shiftId, discount: 2m),
            CancellationToken.None);

        await act.Should().ThrowAsync<FluentValidation.ValidationException>()
            .WithMessage("*discount*");
    }

    [Fact]
    public async Task Authorized_discount_persists_manager_attribution()
    {
        var (context, handler, productId, registerId, shiftId) = await SetupAsync();

        await handler.Handle(
            Sale(productId, registerId, shiftId, discount: 2m, authorizedBy: "manager-1"),
            CancellationToken.None);

        (await context.SaleLines.SingleAsync()).DiscountAuthorizedBy.Should().Be("manager-1");
    }

    [Fact]
    public void Sale_command_participates_in_sensitive_action_auditing()
    {
        new CreateSaleCommand().Should().BeAssignableTo<IAuditedCommand>();
    }

    public void Dispose() => _db.Dispose();
}
