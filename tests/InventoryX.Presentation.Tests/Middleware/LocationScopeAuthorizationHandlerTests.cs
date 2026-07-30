using FluentAssertions;
using InventoryX.Application.Commands.Requests.Inventory;
using InventoryX.Application.Repository;
using InventoryX.Common.Tests;
using InventoryX.Presentation.Middleware;
using Moq;

namespace InventoryX.Presentation.Tests.Middleware;

public sealed class LocationScopeAuthorizationHandlerTests
{
    [Fact]
    public async Task Scoped_manager_is_denied_outside_scope_and_allowed_inside_scope()
    {
        var allowed = Guid.NewGuid();
        var denied = Guid.NewGuid();
        var tenant = new TestTenantContext { Role = "Manager", LocationScope = allowed.ToString() };
        var behavior = new LocationScopeAuthorizationHandler<RecordStockAdjustmentCommand, RecordStockAdjustmentResult>(
            tenant, Mock.Of<IAppDbContext>());

        var outside = () => behavior.Handle(
            new RecordStockAdjustmentCommand { LocationId = denied },
            _ => Task.FromResult(new RecordStockAdjustmentResult("Applied", [])), CancellationToken.None);
        await outside.Should().ThrowAsync<InventoryX.Application.Exceptions.CustomException>()
            .Where(exception => exception.StatusCode == 403);

        var result = await behavior.Handle(
            new RecordStockAdjustmentCommand { LocationId = allowed },
            _ => Task.FromResult(new RecordStockAdjustmentResult("Applied", [])), CancellationToken.None);
        result.Status.Should().Be("Applied");
    }
}
