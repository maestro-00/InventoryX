using FluentAssertions;
using InventoryX.Application.Commands.RequestHandlers.Selling;
using InventoryX.Application.Commands.Requests.Selling;
using InventoryX.Application.Exceptions;
using InventoryX.Common.Tests;
using InventoryX.Domain.Models.Inventory;

namespace InventoryX.Application.Tests.Selling;

/// <summary>T041 - register creation and one-open-shift enforcement.</summary>
public sealed class RegisterShiftCommandTests : IDisposable
{
    private readonly TestDb _db = new(Guid.NewGuid(), "cashier-1");

    [Fact]
    public async Task Creates_register_and_rejects_second_open_shift()
    {
        await using var context = _db.CreateContext();
        var location = new Location { Name = "Main Shop" };
        context.Locations.Add(location);
        await context.SaveChangesAsync();

        var register = await new CreateRegisterCommandHandler(context).Handle(
            new CreateRegisterCommand { LocationId = location.Id, Name = "Register 1" },
            CancellationToken.None);

        var handler = new OpenShiftCommandHandler(context, _db.TenantContext);
        var shift = await handler.Handle(
            new OpenShiftCommand { RegisterId = register.Id, OpeningFloat = 100m },
            CancellationToken.None);

        shift.OpenedBy.Should().Be("cashier-1");
        shift.OpeningFloat.Should().Be(100m);

        var secondOpen = () => handler.Handle(
            new OpenShiftCommand { RegisterId = register.Id, OpeningFloat = 50m },
            CancellationToken.None);
        await secondOpen.Should().ThrowAsync<ConflictException>();
    }

    public void Dispose() => _db.Dispose();
}
