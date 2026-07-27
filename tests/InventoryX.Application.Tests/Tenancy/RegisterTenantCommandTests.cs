using FluentAssertions;
using InventoryX.Application.Commands.RequestHandlers.Tenancy;
using InventoryX.Application.Commands.Requests.Tenancy;
using InventoryX.Common.Tests;
using InventoryX.Domain.Models.Tenancy;
using InventoryX.Infrastructure.Data.Seed;
using Microsoft.EntityFrameworkCore;
using Moq;
using InventoryX.Application.Services.IServices;
using InventoryX.Domain.Models;

namespace InventoryX.Application.Tests.Tenancy;

/// <summary>
/// T027 — registration must create tenant + owner + Trialing Professional
/// subscription with business-type defaults (FR-001, FR-011).
/// </summary>
public sealed class RegisterTenantCommandTests : IDisposable
{
    private readonly TestDb _db = new();

    private RegisterTenantCommandHandler CreateHandler(Infrastructure.Data.AppDbContext context)
    {
        var tokenService = new Mock<ITokenService>();
        tokenService.Setup(t => t.CreateTokenPair(It.IsAny<User>(), It.IsAny<Role>()))
            .Returns(new TokenPair("access", DateTime.UtcNow.AddMinutes(30), "refresh", DateTime.UtcNow.AddDays(14)));
        return new RegisterTenantCommandHandler(context, TestDb.CreateUserManager(context), tokenService.Object, _db.TenantContext);
    }

    private static RegisterTenantCommand NewCommand(string businessType = "Retail") => new()
    {
        Email = "owner@shop.gh",
        Password = "Password1!",
        BusinessName = "Accra Corner Shop",
        Country = "GH",
        Currency = "GHS",
        BusinessType = businessType,
    };

    [Fact]
    public async Task Creates_tenant_owner_and_trialing_professional_subscription()
    {
        await using var context = _db.CreateContext();
        await RoleSeeder.SeedAsync(context);
        await PlanSeeder.SeedAsync(context);

        var result = await CreateHandler(context).Handle(NewCommand(), CancellationToken.None);

        var tenant = await context.Tenants.SingleAsync();
        tenant.Name.Should().Be("Accra Corner Shop");
        tenant.Currency.Should().Be("GHS");

        var owner = await context.Users.SingleAsync();
        owner.IsOwner.Should().BeTrue();
        owner.TenantId.Should().Be(tenant.Id);

        var subscription = await context.Subscriptions.IgnoreQueryFilters().SingleAsync();
        subscription.Status.Should().Be(SubscriptionStatus.Trialing);
        subscription.TrialEndsAt.Should().BeCloseTo(DateTime.UtcNow.AddDays(14), TimeSpan.FromMinutes(5));

        var plan = await context.PlanDefinitions.SingleAsync(p => p.Id == subscription.PlanDefinitionId);
        plan.Tier.Should().Be(PlanTier.Professional);

        result.TenantId.Should().Be(tenant.Id);
        result.AccessToken.Should().NotBeNullOrEmpty();

        tenant.OnboardingChecklist.Should().Contain("createLocation");
    }

    [Fact]
    public async Task Pharmacy_business_type_defaults_to_expiry_required()
    {
        await using var context = _db.CreateContext();
        await RoleSeeder.SeedAsync(context);
        await PlanSeeder.SeedAsync(context);

        await CreateHandler(context).Handle(NewCommand("Pharmacy"), CancellationToken.None);

        var tenant = await context.Tenants.SingleAsync();
        tenant.BusinessType.Should().Be(BusinessType.Pharmacy);
        tenant.RequireExpiryOnBatchReceipt.Should().BeTrue();
    }

    [Fact]
    public async Task Duplicate_email_is_rejected()
    {
        await using var context = _db.CreateContext();
        await RoleSeeder.SeedAsync(context);
        await PlanSeeder.SeedAsync(context);

        var handler = CreateHandler(context);
        await handler.Handle(NewCommand(), CancellationToken.None);

        var act = () => handler.Handle(NewCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<Exceptions.ConflictException>();
    }

    public void Dispose() => _db.Dispose();
}
