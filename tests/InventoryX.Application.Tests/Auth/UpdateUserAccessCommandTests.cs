using FluentAssertions;
using InventoryX.Application.Commands.RequestHandlers.Auth;
using InventoryX.Application.Commands.Requests.Auth;
using InventoryX.Application.Exceptions;
using InventoryX.Common.Tests;
using InventoryX.Domain.Models;
using InventoryX.Domain.Models.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace InventoryX.Application.Tests.Auth;

public sealed class UpdateUserAccessCommandTests : IDisposable
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly TestDb _db;

    public UpdateUserAccessCommandTests()
    {
        _db = new TestDb(_tenantId);
    }

    [Fact]
    public async Task Updates_role_location_scope_and_status()
    {
        await using var context = _db.CreateContext();
        var roleId = Guid.NewGuid();
        var user = new User
        {
            Id = "user-1",
            TenantId = _tenantId,
            UserName = "staff@example.com",
            Email = "staff@example.com",
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var handler = new UpdateUserAccessCommandHandler(
            context, _db.TenantContext, TestPosAccess.For(context, _db.TenantContext, "Admin", Permission.ManageUsers));
        await handler.Handle(new UpdateUserAccessCommand
        {
            UserId = user.Id,
            RoleId = roleId,
            LocationScope = "location-1",
            Status = UserStatus.Deactivated,
        }, CancellationToken.None);

        var stored = await context.Users.SingleAsync();
        stored.RoleId.Should().Be(roleId);
        stored.LocationScope.Should().Be("location-1");
        stored.Status.Should().Be(UserStatus.Deactivated);
    }

    [Fact]
    public async Task Tenant_owner_cannot_be_deactivated()
    {
        await using var context = _db.CreateContext();
        context.Users.Add(new User
        {
            Id = "owner-1",
            TenantId = _tenantId,
            UserName = "owner@example.com",
            Email = "owner@example.com",
            IsOwner = true,
        });
        await context.SaveChangesAsync();

        var handler = new UpdateUserAccessCommandHandler(
            context, _db.TenantContext, TestPosAccess.For(context, _db.TenantContext, "Admin", Permission.ManageUsers));
        var act = () => handler.Handle(new UpdateUserAccessCommand
        {
            UserId = "owner-1",
            Status = UserStatus.Deactivated,
        }, CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Rejects_cross_tenant_user_update()
    {
        var otherTenantId = Guid.NewGuid();
        await using var context = _db.CreateContext();
        context.Users.Add(new User
        {
            Id = "other-tenant-user",
            TenantId = otherTenantId,
            UserName = "other@example.com",
            Email = "other@example.com",
        });
        await context.SaveChangesAsync();

        var handler = new UpdateUserAccessCommandHandler(
            context, _db.TenantContext, TestPosAccess.For(context, _db.TenantContext, "Admin", Permission.ManageUsers));
        var act = () => handler.Handle(new UpdateUserAccessCommand
        {
            UserId = "other-tenant-user",
            Status = UserStatus.Deactivated,
        }, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    public void Dispose() => _db.Dispose();
}
