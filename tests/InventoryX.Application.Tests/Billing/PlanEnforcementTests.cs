using FluentAssertions;
using InventoryX.Application.Exceptions;
using InventoryX.Application.Services.IServices;
using InventoryX.Common.Tests;
using InventoryX.Domain.Models.Tenancy;
using InventoryX.Infrastructure.Services;

namespace InventoryX.Application.Tests.Billing;

/// <summary>T074 - plan limits, feature gates, and read-only write policy.</summary>
public sealed class PlanEnforcementTests : IDisposable
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly TestDb _db;

    public PlanEnforcementTests() => _db = new TestDb(_tenantId, "owner-1");

    [Fact]
    public async Task Free_plan_rejects_301st_sale_with_upgrade_hint()
    {
        await using var context = _db.CreateContext();
        var plan = new PlanDefinition { Name = "Free", Tier = PlanTier.Free, MonthlySaleCap = 300 };
        context.PlanDefinitions.Add(plan);
        context.Subscriptions.Add(new Subscription
        {
            PlanDefinitionId = plan.Id, Status = SubscriptionStatus.Active,
            CurrentPeriodStart = DateTime.UtcNow.AddDays(-1), CurrentPeriodEnd = DateTime.UtcNow.AddMonths(1),
        });
        context.UsageCounters.Add(new UsageCounter
        {
            Metric = UsageMetric.SalesThisMonth, PeriodKey = DateTime.UtcNow.ToString("yyyy-MM"), Count = 300,
        });
        await context.SaveChangesAsync();

        var act = () => new PlanEnforcer(context, _db.TenantContext)
            .EnsureWithinLimitAsync(UsageMetric.SalesThisMonth);
        var exception = await act.Should().ThrowAsync<PlanLimitException>();
        exception.Which.UpgradeHint.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Disabled_plan_module_is_rejected()
    {
        await using var context = _db.CreateContext();
        var plan = new PlanDefinition { Name = "Free", Tier = PlanTier.Free, PurchaseOrders = false };
        context.PlanDefinitions.Add(plan);
        context.Subscriptions.Add(new Subscription
        {
            PlanDefinitionId = plan.Id, Status = SubscriptionStatus.Active,
            CurrentPeriodStart = DateTime.UtcNow, CurrentPeriodEnd = DateTime.UtcNow.AddMonths(1),
        });
        await context.SaveChangesAsync();

        var act = () => new PlanEnforcer(context, _db.TenantContext).EnsureFeatureAsync(PlanFeature.PurchaseOrders);
        await act.Should().ThrowAsync<PlanLimitException>();
    }

    [Fact]
    public void Read_only_policy_defines_explicit_export_and_billing_write_exemptions()
    {
        Type.GetType("InventoryX.Application.Behaviors.IReadOnlyWriteExemptCommand, InventoryX.Application")
            .Should().NotBeNull("export and billing commands must remain usable in ReadOnly state");
    }

    public void Dispose() => _db.Dispose();
}
