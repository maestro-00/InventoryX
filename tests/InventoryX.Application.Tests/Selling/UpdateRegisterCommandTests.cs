using FluentAssertions;
using InventoryX.Application.Commands.RequestHandlers.Selling;
using InventoryX.Application.Commands.Requests.Selling;
using InventoryX.Application.Exceptions;
using InventoryX.Common.Tests;
using InventoryX.Domain.Models.Inventory;

namespace InventoryX.Application.Tests.Selling;

public sealed class UpdateRegisterCommandTests : IDisposable
{
    private readonly TestDb _db = new(Guid.NewGuid(), "admin-1");

    [Fact]
    public async Task Updates_name_and_rejects_stale_etag()
    {
        await using var context = _db.CreateContext();
        var location = new Location { Name = "Shop" };
        context.Locations.Add(location);
        await context.SaveChangesAsync();

        var created = await new CreateRegisterCommandHandler(context).Handle(
            new CreateRegisterCommand { LocationId = location.Id, Name = "Till 1" }, CancellationToken.None);

        var updated = await new UpdateRegisterCommandHandler(context).Handle(
            new UpdateRegisterCommand { Id = created.Id, Name = "Till A" }, CancellationToken.None);
        updated.Name.Should().Be("Till A");

        var stale = () => new UpdateRegisterCommandHandler(context).Handle(
            new UpdateRegisterCommand
            {
                Id = created.Id,
                Name = "Till B",
                ExpectedRowVersion = [1, 2, 3, 4],
            }, CancellationToken.None);
        await stale.Should().ThrowAsync<ConflictException>();
    }

    public void Dispose() => _db.Dispose();
}
