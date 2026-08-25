using FluentAssertions;
using InventoryX.Application.Exceptions;
using InventoryX.Application.Queries.RequestHandlers.Users;
using InventoryX.Application.DTOs.Users;
using InventoryX.Common.Tests;
using InventoryX.Domain.Models;
using InventoryX.Domain.Models.Tenancy;

namespace InventoryX.Application.Tests.Auth;

public sealed class GetUsersQueryTests : IDisposable
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _otherTenantId = Guid.NewGuid();
    private readonly TestDb _db;

    public GetUsersQueryTests() => _db = new TestDb(_tenantId);

    [Fact]
    public async Task Returns_only_users_in_callers_tenant()
    {
        await using var context = _db.CreateContext();
        context.Users.AddRange(
            new User { Id = "u1", TenantId = _tenantId, UserName = "a@example.com", Email = "a@example.com" },
            new User { Id = "u2", TenantId = _otherTenantId, UserName = "b@example.com", Email = "b@example.com" });
        await context.SaveChangesAsync();

        var posAccess = TestPosAccess.For(context, _db.TenantContext, "Admin", Permission.ManageUsers);
        var handler = new GetUsersQueryHandler(context, _db.TenantContext, posAccess);
        var result = await handler.Handle(new GetUsersQuery { Page = 1, PageSize = 20 }, CancellationToken.None);

        result.Items.Should().HaveCount(1);
        result.Items[0].Email.Should().Be("a@example.com");
    }

    [Fact]
    public async Task Requires_manage_users_permission()
    {
        await using var context = _db.CreateContext();
        var posAccess = TestPosAccess.Cashier(context, _db.TenantContext);
        var handler = new GetUsersQueryHandler(context, _db.TenantContext, posAccess);
        var act = () => handler.Handle(new GetUsersQuery { Page = 1, PageSize = 20 }, CancellationToken.None);
        await act.Should().ThrowAsync<CustomException>().Where(ex => ex.StatusCode == 403);
    }

    public void Dispose() => _db.Dispose();
}
