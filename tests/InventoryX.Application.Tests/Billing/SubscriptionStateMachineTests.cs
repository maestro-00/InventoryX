using FluentAssertions;
using InventoryX.Application.Services;
using InventoryX.Domain.Models.Tenancy;

namespace InventoryX.Application.Tests.Billing;

public sealed class SubscriptionStateMachineTests
{
    [Fact]
    public void Grace_starts_once_and_does_not_extend_on_repeated_failures()
    {
        var due = new DateTime(2026, 8, 1, 10, 0, 0, DateTimeKind.Utc);
        var subscription = new Subscription { Status = SubscriptionStatus.Active };

        SubscriptionStateMachine.RecordChargeFailure(subscription, due);
        SubscriptionStateMachine.RecordChargeFailure(subscription, due.AddDays(2));

        subscription.Status.Should().Be(SubscriptionStatus.PastDue);
        subscription.GraceExpiresAt.Should().Be(due.AddDays(7));
        subscription.FailedChargeAttempts.Should().Be(2);
    }

    [Fact]
    public void Grace_expires_to_read_only_at_exact_deadline()
    {
        var deadline = new DateTime(2026, 8, 8, 10, 0, 0, DateTimeKind.Utc);
        var subscription = new Subscription
        {
            Status = SubscriptionStatus.PastDue,
            GraceExpiresAt = deadline,
        };

        SubscriptionStateMachine.ExhaustGrace(subscription, deadline.AddTicks(-1));
        subscription.Status.Should().Be(SubscriptionStatus.PastDue);
        SubscriptionStateMachine.ExhaustGrace(subscription, deadline);
        subscription.Status.Should().Be(SubscriptionStatus.ReadOnly);
    }

    [Fact]
    public void Cancellation_retention_never_starts_before_paid_access_ends()
    {
        var end = new DateTime(2026, 8, 31, 10, 0, 0, DateTimeKind.Utc);
        var subscription = new Subscription
        {
            Status = SubscriptionStatus.Active,
            CurrentPeriodEnd = end,
        };

        SubscriptionStateMachine.CancelAtPeriodEnd(subscription, end.AddDays(-5));

        subscription.CancelledAt.Should().Be(end);
        subscription.PurgeAt.Should().Be(end.AddDays(90));
    }

    [Fact]
    public void Trial_expiry_moves_to_free_without_deleting_data()
    {
        var now = new DateTime(2026, 8, 15, 10, 0, 0, DateTimeKind.Utc);
        var subscription = new Subscription
        {
            Status = SubscriptionStatus.Trialing,
            TrialEndsAt = now.AddTicks(-1),
            CurrentPeriodStart = now.AddDays(-14),
        };

        SubscriptionStateMachine.ExpireTrialToFree(subscription, Guid.NewGuid(), now);

        subscription.Status.Should().Be(SubscriptionStatus.Active);
        subscription.TrialEndsAt.Should().BeNull();
        subscription.PurgeAt.Should().BeNull();
    }
}
