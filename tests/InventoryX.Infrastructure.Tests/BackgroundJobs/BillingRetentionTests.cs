using FluentAssertions;
using InventoryX.Application.Services;
using InventoryX.Domain.Models.Tenancy;

namespace InventoryX.Infrastructure.Tests.BackgroundJobs;

public sealed class BillingRetentionTests
{
    [Fact]
    public void Purge_clock_starts_at_least_90_days_after_paid_access_ends()
    {
        var endedAt = new DateTime(2026, 8, 31, 10, 0, 0, DateTimeKind.Utc);
        var subscription = new Subscription
        {
            Status = SubscriptionStatus.Cancelled,
            CancelledAt = endedAt,
        };

        SubscriptionStateMachine.StartPurgeClock(subscription, endedAt);

        subscription.Status.Should().Be(SubscriptionStatus.PurgePending);
        subscription.PurgeAt.Should().Be(endedAt.AddDays(90));
    }

    [Fact]
    public void Reactivation_before_purge_clears_cancellation_retention_state()
    {
        var now = new DateTime(2026, 8, 31, 10, 0, 0, DateTimeKind.Utc);
        var subscription = new Subscription
        {
            Status = SubscriptionStatus.PurgePending,
            CancelledAt = now.AddDays(-10),
            PurgeAt = now.AddDays(80),
        };

        SubscriptionStateMachine.Reactivate(subscription, now);

        subscription.Status.Should().Be(SubscriptionStatus.Active);
        subscription.CancelledAt.Should().BeNull();
        subscription.PurgeAt.Should().BeNull();
    }
}
