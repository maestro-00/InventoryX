using FluentAssertions;
using InventoryX.Application.Queries.RequestHandlers.Selling;
using InventoryX.Application.Queries.Requests.Selling;
using InventoryX.Application.Services;
using InventoryX.Common.Tests;
using InventoryX.Domain.Models.Inventory;
using InventoryX.Domain.Models.Selling;

namespace InventoryX.Application.Tests.Selling;

/// <summary>Open shifts must be fetchable so a POS can resume after restart.</summary>
public sealed class GetShiftsQueryTests : IDisposable
{
    private readonly TestDb _db = new(Guid.NewGuid(), "cashier-1");

    [Fact]
    public async Task Returns_open_shifts_so_a_pos_can_resume()
    {
        await using var context = _db.CreateContext();
        var location = new Location { Name = "Main Shop" };
        var register = new Register { Name = "Register 1", LocationId = location.Id };
        var otherRegister = new Register { Name = "Register 2", LocationId = location.Id };
        context.Locations.Add(location);
        context.Registers.AddRange(register, otherRegister);
        await context.SaveChangesAsync();

        var open = new Shift
        {
            RegisterId = register.Id,
            OpenedBy = "cashier-1",
            OpenedAt = DateTime.UtcNow.AddHours(-2),
            OpeningFloat = 100m,
            Status = ShiftStatus.Open,
        };
        var closed = new Shift
        {
            RegisterId = register.Id,
            OpenedBy = "cashier-1",
            OpenedAt = DateTime.UtcNow.AddDays(-1),
            OpeningFloat = 80m,
            Status = ShiftStatus.Closed,
            ClosedAt = DateTime.UtcNow.AddDays(-1).AddHours(8),
            ClosedBy = "cashier-1",
        };
        var otherOpen = new Shift
        {
            RegisterId = otherRegister.Id,
            OpenedBy = "cashier-2",
            OpenedAt = DateTime.UtcNow.AddMinutes(-30),
            OpeningFloat = 50m,
            Status = ShiftStatus.Open,
        };
        context.Shifts.AddRange(open, closed, otherOpen);
        await context.SaveChangesAsync();

        TestPosAccess.Cashier(context, _db.TenantContext);
        var handler = new GetShiftsQueryHandler(context, new PosAccess(context, _db.TenantContext));

        var forRegister = await handler.Handle(
            new GetShiftsQuery { RegisterId = register.Id, Status = "Open" },
            CancellationToken.None);

        forRegister.Should().ContainSingle();
        forRegister[0].Id.Should().Be(open.Id);
        forRegister[0].RegisterId.Should().Be(register.Id);
        forRegister[0].OpenedBy.Should().Be("cashier-1");
        forRegister[0].OpeningFloat.Should().Be(100m);
        forRegister[0].Status.Should().Be("Open");

        var allOpen = await handler.Handle(
            new GetShiftsQuery { Status = "Open" },
            CancellationToken.None);

        allOpen.Select(s => s.Id).Should().BeEquivalentTo([open.Id]);
        allOpen.Should().BeInDescendingOrder(s => s.OpenedAt);
    }

    [Fact]
    public async Task Status_filter_is_case_insensitive_and_omitted_status_returns_all()
    {
        await using var context = _db.CreateContext();
        var location = new Location { Name = "Main Shop" };
        var register = new Register { Name = "Register 1", LocationId = location.Id };
        context.AddRange(location, register);
        await context.SaveChangesAsync();
        context.Shifts.AddRange(
            new Shift
            {
                RegisterId = register.Id,
                OpenedBy = "cashier-1",
                OpenedAt = DateTime.UtcNow,
                OpeningFloat = 100m,
                Status = ShiftStatus.Open,
            },
            new Shift
            {
                RegisterId = register.Id,
                OpenedBy = "cashier-1",
                OpenedAt = DateTime.UtcNow.AddHours(-5),
                OpeningFloat = 80m,
                Status = ShiftStatus.Closed,
            });
        await context.SaveChangesAsync();

        TestPosAccess.Cashier(context, _db.TenantContext);
        var handler = new GetShiftsQueryHandler(context, new PosAccess(context, _db.TenantContext));

        (await handler.Handle(new GetShiftsQuery { Status = "open" }, CancellationToken.None))
            .Should().ContainSingle(s => s.Status == "Open");
        (await handler.Handle(new GetShiftsQuery { Status = "CLOSED" }, CancellationToken.None))
            .Should().ContainSingle(s => s.Status == "Closed");
        (await handler.Handle(new GetShiftsQuery(), CancellationToken.None))
            .Should().HaveCount(2);
    }

    public void Dispose() => _db.Dispose();
}
