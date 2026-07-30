using FluentAssertions;
using InventoryX.Application.Commands.RequestHandlers.Billing;
using InventoryX.Application.Commands.Requests.Billing;
using InventoryX.Common.Tests;
using InventoryX.Domain.Models.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace InventoryX.Application.Tests.Billing;

public sealed class SubscriptionCommandTests : IDisposable
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly TestDb _db;

    public SubscriptionCommandTests() => _db = new TestDb(_tenantId, "owner-1");

    [Fact]
    public async Task Downgrade_requires_acknowledgement_when_usage_exceeds_target_limit_and_cancellation_reactivates()
    {
        await using var context = _db.CreateContext();
        var source = new PlanDefinition { Name = "Professional", Tier = PlanTier.Professional };
        var target = new PlanDefinition { Name = "Free", Tier = PlanTier.Free, MaxProducts = 1 };
        context.AddRange(source, target);
        context.Subscriptions.Add(new Subscription
        {
            PlanDefinitionId = source.Id, Status = SubscriptionStatus.Active,
            CurrentPeriodStart = DateTime.UtcNow.AddDays(-5), CurrentPeriodEnd = DateTime.UtcNow.AddDays(25),
        });
        context.UsageCounters.Add(new UsageCounter { Metric = UsageMetric.Products, Count = 2 });
        await context.SaveChangesAsync();

        var downgrade = new DowngradeSubscriptionCommandHandler(context);
        var blocked = () => downgrade.Handle(new DowngradeSubscriptionCommand { PlanDefinitionId = target.Id }, CancellationToken.None);
        await blocked.Should().ThrowAsync<InventoryX.Application.Exceptions.ConflictException>();
        var scheduled = await downgrade.Handle(new DowngradeSubscriptionCommand { PlanDefinitionId = target.Id, AcknowledgeOverLimit = true }, CancellationToken.None);
        (await context.Subscriptions.SingleAsync()).PendingPlanDefinitionId.Should().Be(target.Id);

        var cancelled = await new CancelSubscriptionCommandHandler(context).Handle(new CancelSubscriptionCommand(), CancellationToken.None);
        cancelled.PurgeAt.Should().NotBeNull();
        var active = await new ReactivateSubscriptionCommandHandler(context).Handle(new ReactivateSubscriptionCommand(), CancellationToken.None);
        active.CancelledAt.Should().BeNull();
    }

    public void Dispose() => _db.Dispose();
}
