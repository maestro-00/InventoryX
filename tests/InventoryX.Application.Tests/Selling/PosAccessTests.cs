using FluentAssertions;
using InventoryX.Application.Commands.RequestHandlers.Selling;
using InventoryX.Application.Commands.Requests.Selling;
using InventoryX.Application.Exceptions;
using InventoryX.Application.Queries.RequestHandlers.Selling;
using InventoryX.Application.Queries.Requests.Selling;
using InventoryX.Application.Services;
using InventoryX.Application.Services.IServices;
using InventoryX.Common.Tests;
using InventoryX.Domain.Models.Catalog;
using InventoryX.Domain.Models.Inventory;
using InventoryX.Domain.Models.Selling;
using InventoryX.Domain.Models.Tenancy;
using InventoryX.Infrastructure.Data;
using InventoryX.Infrastructure.Services;
using Moq;

namespace InventoryX.Application.Tests.Selling;

public sealed class PosAccessTests : IDisposable
{
    private readonly TestDb _db = new(Guid.NewGuid(), "cashier-1");

    [Fact]
    public async Task Cashier_lists_only_own_open_shift()
    {
        await using var context = _db.CreateContext();
        var (own, other) = await SeedTwoOpenShiftsAsync(context);
        var access = TestPosAccess.Cashier(context, _db.TenantContext);

        var listed = await new GetShiftsQueryHandler(context, access).Handle(
            new GetShiftsQuery { Status = "Open" }, CancellationToken.None);

        listed.Should().ContainSingle();
        listed[0].Id.Should().Be(own.Id);
        listed.Should().NotContain(item => item.Id == other.Id);
    }

    [Fact]
    public async Task Manager_lists_all_open_shifts()
    {
        await using var context = _db.CreateContext();
        var (own, other) = await SeedTwoOpenShiftsAsync(context);
        _db.TenantContext.UserId = "manager-1";
        var access = TestPosAccess.Manager(context, _db.TenantContext);

        var listed = await new GetShiftsQueryHandler(context, access).Handle(
            new GetShiftsQuery { Status = "Open" }, CancellationToken.None);

        listed.Select(item => item.Id).Should().BeEquivalentTo([own.Id, other.Id]);
    }

    [Fact]
    public async Task Cashier_cannot_sell_on_another_users_open_shift()
    {
        await using var context = _db.CreateContext();
        var (_, other) = await SeedTwoOpenShiftsAsync(context);
        var product = context.Products.First();
        TestPosAccess.Cashier(context, _db.TenantContext);
        var handler = new CreateSaleCommandHandler(
            context, new StockLedger(context), new TaxCalculator(), _db.TenantContext,
            Mock.Of<IPlanEnforcer>(), new PosAccess(context, _db.TenantContext));

        var act = () => handler.Handle(new CreateSaleCommand
        {
            ClientSaleId = Guid.NewGuid(),
            RegisterId = other.RegisterId,
            ShiftId = other.Id,
            Lines = [new CreateSaleLineDto { ProductId = product.Id, Qty = 1m }],
            Payments = [new CreateSalePaymentDto { Tender = "Cash", Amount = 10m }],
        }, CancellationToken.None);

        var error = await act.Should().ThrowAsync<CustomException>();
        error.Which.StatusCode.Should().Be(403);
    }

    [Fact]
    public async Task Manager_can_sell_on_another_users_open_shift()
    {
        await using var context = _db.CreateContext();
        var (_, other) = await SeedTwoOpenShiftsAsync(context);
        var product = context.Products.First();
        _db.TenantContext.UserId = "manager-1";
        TestPosAccess.Manager(context, _db.TenantContext);
        var handler = new CreateSaleCommandHandler(
            context, new StockLedger(context), new TaxCalculator(), _db.TenantContext,
            Mock.Of<IPlanEnforcer>(), new PosAccess(context, _db.TenantContext));

        var sale = await handler.Handle(new CreateSaleCommand
        {
            ClientSaleId = Guid.NewGuid(),
            RegisterId = other.RegisterId,
            ShiftId = other.Id,
            Lines = [new CreateSaleLineDto { ProductId = product.Id, Qty = 1m }],
            Payments = [new CreateSalePaymentDto { Tender = "Cash", Amount = 10m }],
        }, CancellationToken.None);

        sale.CashierId.Should().Be("manager-1");
        sale.ShiftId.Should().Be(other.Id);
        other.OpenedBy.Should().Be("cashier-2");
    }

    [Fact]
    public async Task Cashier_sales_list_is_own_only_and_other_sale_is_hidden()
    {
        await using var context = _db.CreateContext();
        var (ownShift, otherShift) = await SeedTwoOpenShiftsAsync(context);
        context.Sales.AddRange(
            new Sale
            {
                LocationId = context.Locations.First().Id,
                RegisterId = ownShift.RegisterId,
                ShiftId = ownShift.Id,
                CashierId = "cashier-1",
                ClientSaleId = Guid.NewGuid(),
                OccurredAt = DateTime.UtcNow,
            },
            new Sale
            {
                LocationId = context.Locations.First().Id,
                RegisterId = otherShift.RegisterId,
                ShiftId = otherShift.Id,
                CashierId = "cashier-2",
                ClientSaleId = Guid.NewGuid(),
                OccurredAt = DateTime.UtcNow,
            });
        await context.SaveChangesAsync();
        var otherId = context.Sales.Single(item => item.CashierId == "cashier-2").Id;
        var access = TestPosAccess.Cashier(context, _db.TenantContext);

        var page = await new GetSalesQueryHandler(context, access).Handle(new GetSalesQuery(), CancellationToken.None);
        page.Items.Should().ContainSingle(item => item.CashierId == "cashier-1");

        var getOther = () => new GetSaleQueryHandler(context, access).Handle(
            new GetSaleQuery { Id = otherId }, CancellationToken.None);
        await getOther.Should().ThrowAsync<NotFoundException>();

        _db.TenantContext.UserId = "manager-1";
        var manager = TestPosAccess.Manager(context, _db.TenantContext);
        var all = await new GetSalesQueryHandler(context, manager).Handle(new GetSalesQuery(), CancellationToken.None);
        all.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task Read_only_can_list_sales_but_not_shifts_or_create_sales()
    {
        await using var context = _db.CreateContext();
        var (ownShift, _) = await SeedTwoOpenShiftsAsync(context);
        context.Sales.Add(new Sale
        {
            LocationId = context.Locations.First().Id,
            RegisterId = ownShift.RegisterId,
            ShiftId = ownShift.Id,
            CashierId = "cashier-1",
            ClientSaleId = Guid.NewGuid(),
            OccurredAt = DateTime.UtcNow,
        });
        await context.SaveChangesAsync();
        _db.TenantContext.UserId = "accountant-1";
        var access = TestPosAccess.ReadOnly(context, _db.TenantContext);

        var sales = await new GetSalesQueryHandler(context, access).Handle(new GetSalesQuery(), CancellationToken.None);
        sales.Items.Should().HaveCount(1);

        var listShifts = () => new GetShiftsQueryHandler(context, access).Handle(
            new GetShiftsQuery { Status = "Open" }, CancellationToken.None);
        (await listShifts.Should().ThrowAsync<CustomException>()).Which.StatusCode.Should().Be(403);

        var product = context.Products.First();
        var create = new CreateSaleCommandHandler(
            context, new StockLedger(context), new TaxCalculator(), _db.TenantContext,
            Mock.Of<IPlanEnforcer>(), new PosAccess(context, _db.TenantContext));
        var sell = () => create.Handle(new CreateSaleCommand
        {
            ClientSaleId = Guid.NewGuid(),
            RegisterId = ownShift.RegisterId,
            ShiftId = ownShift.Id,
            Lines = [new CreateSaleLineDto { ProductId = product.Id, Qty = 1m }],
            Payments = [new CreateSalePaymentDto { Tender = "Cash", Amount = 10m }],
        }, CancellationToken.None);
        (await sell.Should().ThrowAsync<CustomException>()).Which.StatusCode.Should().Be(403);
    }

    [Fact]
    public async Task Cashier_held_list_is_own_only_and_other_held_sale_is_hidden()
    {
        await using var context = _db.CreateContext();
        var (ownShift, otherShift) = await SeedTwoOpenShiftsAsync(context);
        var locationId = context.Locations.First().Id;
        context.Sales.AddRange(
            new Sale
            {
                LocationId = locationId,
                RegisterId = ownShift.RegisterId,
                ShiftId = ownShift.Id,
                CashierId = "cashier-1",
                ClientSaleId = Guid.NewGuid(),
                Status = SaleStatus.Held,
                OccurredAt = DateTime.UtcNow,
            },
            new Sale
            {
                LocationId = locationId,
                RegisterId = otherShift.RegisterId,
                ShiftId = otherShift.Id,
                CashierId = "cashier-2",
                ClientSaleId = Guid.NewGuid(),
                Status = SaleStatus.Held,
                OccurredAt = DateTime.UtcNow,
            });
        await context.SaveChangesAsync();
        var otherHeldId = context.Sales.Single(item => item.CashierId == "cashier-2").Id;
        var access = TestPosAccess.Cashier(context, _db.TenantContext);

        var held = await new GetHeldSalesQueryHandler(context, access).Handle(
            new GetHeldSalesQuery(), CancellationToken.None);
        held.Should().ContainSingle(item => item.CashierId == "cashier-1");

        var getOther = () => new GetHeldSaleQueryHandler(context, access).Handle(
            new GetHeldSaleQuery { Id = otherHeldId }, CancellationToken.None);
        await getOther.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Cashier_cannot_move_cash_or_view_z_report_on_another_users_shift()
    {
        await using var context = _db.CreateContext();
        var (_, other) = await SeedTwoOpenShiftsAsync(context);
        var access = TestPosAccess.Cashier(context, _db.TenantContext);

        var cash = () => new RecordCashMovementCommandHandler(context, _db.TenantContext, access).Handle(
            new RecordCashMovementCommand { ShiftId = other.Id, Type = "CashOut", Amount = 5m, Reason = "PettyCash" },
            CancellationToken.None);
        (await cash.Should().ThrowAsync<CustomException>()).Which.StatusCode.Should().Be(403);

        var zReport = () => new GetZReportQueryHandler(context, access).Handle(
            new GetZReportQuery(other.Id), CancellationToken.None);
        (await zReport.Should().ThrowAsync<CustomException>()).Which.StatusCode.Should().Be(403);
    }

    [Fact]
    public async Task Manager_can_move_cash_on_another_users_open_shift()
    {
        await using var context = _db.CreateContext();
        var (_, other) = await SeedTwoOpenShiftsAsync(context);
        _db.TenantContext.UserId = "manager-1";
        var access = TestPosAccess.Manager(context, _db.TenantContext);

        var movement = await new RecordCashMovementCommandHandler(context, _db.TenantContext, access).Handle(
            new RecordCashMovementCommand { ShiftId = other.Id, Type = "CashOut", Amount = 5m, Reason = "PettyCash" },
            CancellationToken.None);

        movement.Amount.Should().Be(5m);
        movement.Type.Should().Be("CashOut");
    }

    private async Task<(Shift Own, Shift Other)> SeedTwoOpenShiftsAsync(AppDbContext context)
    {
        var location = new Location { Name = "Main Shop" };
        var product = new Product { Name = "Sugar", SellingPrice = 10m };
        var register = new Register { Name = "R1", LocationId = location.Id };
        var otherRegister = new Register { Name = "R2", LocationId = location.Id };
        var own = new Shift
        {
            RegisterId = register.Id,
            OpenedBy = "cashier-1",
            OpenedAt = DateTime.UtcNow.AddHours(-2),
            OpeningFloat = 100m,
        };
        var other = new Shift
        {
            RegisterId = otherRegister.Id,
            OpenedBy = "cashier-2",
            OpenedAt = DateTime.UtcNow.AddMinutes(-30),
            OpeningFloat = 50m,
        };
        context.AddRange(location, product, register, otherRegister, own, other);
        await context.SaveChangesAsync();
        var ledger = new StockLedger(context);
        await ledger.AppendAsync([new StockMovementRequest(MovementType.Adjustment, product.Id, location.Id, 20m)]);
        await context.SaveChangesAsync();
        return (own, other);
    }

    public void Dispose() => _db.Dispose();
}
