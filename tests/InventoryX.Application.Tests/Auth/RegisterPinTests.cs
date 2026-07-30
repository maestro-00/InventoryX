using FluentAssertions;
using InventoryX.Application.Commands.RequestHandlers.Auth;
using InventoryX.Application.Commands.Requests.Auth;
using InventoryX.Application.Services.IServices;
using InventoryX.Common.Tests;
using InventoryX.Domain.Models;
using InventoryX.Domain.Models.Inventory;
using InventoryX.Domain.Models.Selling;
using InventoryX.Domain.Models.Tenancy;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace InventoryX.Application.Tests.Auth;

public sealed class RegisterPinTests : IDisposable
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly TestDb _db;

    public RegisterPinTests() => _db = new TestDb(_tenantId, "admin-1");

    [Fact]
    public async Task Hashed_pin_exchanges_for_a_register_scoped_token()
    {
        _db.TenantContext.Role = "Administrator";
        await using var context = _db.CreateContext();
        var location = new Location { Name = "Main" };
        var register = new Register { Name = "R1", LocationId = location.Id };
        var role = new Role { Name = "Cashier", Permissions = Permission.Sell, IsSystem = true };
        var user = new User
        {
            Id = "cashier-1", UserName = "cashier@example.com", Email = "cashier@example.com",
            TenantId = _tenantId, RoleId = role.Id, LocationScope = location.Id.ToString(),
        };
        context.AddRange(location, register, role, user);
        await context.SaveChangesAsync();
        var hasher = new PasswordHasher<User>();
        await new SetRegisterPinCommandHandler(context, _db.TenantContext, hasher).Handle(
            new SetRegisterPinCommand { UserId = user.Id, Pin = "2468" }, CancellationToken.None);
        (await context.RegisterPins.SingleAsync()).PasswordHash.Should().NotBe("2468");

        var tokens = new Mock<ITokenService>();
        tokens.Setup(t => t.CreateRegisterScopedToken(user, role, register.Id)).Returns("register-jwt");
        var result = await new ExchangeRegisterPinCommandHandler(context, _db.TenantContext, hasher, tokens.Object).Handle(
            new ExchangeRegisterPinCommand { UserId = user.Id, Pin = "2468", RegisterId = register.Id }, CancellationToken.None);
        result.AccessToken.Should().Be("register-jwt");
    }

    public void Dispose() => _db.Dispose();
}
