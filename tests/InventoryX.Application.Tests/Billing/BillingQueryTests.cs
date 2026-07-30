using FluentAssertions;
using InventoryX.Application.Queries.RequestHandlers.Billing;
using InventoryX.Application.Queries.Requests.Billing;
using InventoryX.Common.Tests;
using InventoryX.Domain.Models.Tenancy;

namespace InventoryX.Application.Tests.Billing;

public sealed class BillingQueryTests : IDisposable
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly TestDb _db;

    public BillingQueryTests() => _db = new TestDb(_tenantId, "owner-1");

    [Fact]
    public async Task Plans_and_subscription_expose_limits_features_and_usage()
    {
        await using var context = _db.CreateContext();
        var plan = new PlanDefinition
        {
            Name = "Standard", Tier = PlanTier.Standard, MonthlyPrice = 199m,
            MaxProducts = 5000, MonthlySaleCap = 3000, PurchaseOrders = true,
        };
        context.PlanDefinitions.Add(plan);
        context.Subscriptions.Add(new Subscription
        {
            PlanDefinitionId = plan.Id, Status = SubscriptionStatus.Active,
            CurrentPeriodStart = DateTime.UtcNow, CurrentPeriodEnd = DateTime.UtcNow.AddMonths(1),
        });
        context.UsageCounters.Add(new UsageCounter
        {
            Metric = UsageMetric.SalesThisMonth, PeriodKey = DateTime.UtcNow.ToString("yyyy-MM"), Count = 12,
        });
        await context.SaveChangesAsync();

        var plans = await new GetBillingPlansQueryHandler(context).Handle(new GetBillingPlansQuery(), CancellationToken.None);
        plans.Should().ContainSingle(item => item.Id == plan.Id && item.Features["purchaseOrders"]);
        var subscription = await new GetCurrentBillingSubscriptionQueryHandler(context)
            .Handle(new GetCurrentBillingSubscriptionQuery(), CancellationToken.None);
        subscription.Plan.Should().Be("Standard");
        subscription.Usage.Should().ContainSingle(item => item.Metric == "salesThisMonth" && item.Current == 12 && item.Limit == 3000);
    }

    public void Dispose() => _db.Dispose();
}
