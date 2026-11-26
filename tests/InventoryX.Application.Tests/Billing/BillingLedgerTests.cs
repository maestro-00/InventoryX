using InventoryX.Application.Services;
using InventoryX.Domain.Models.Tenancy;

namespace InventoryX.Application.Tests.Billing;

public sealed class BillingLedgerTests
{
    [Theory]
    [InlineData("2026-01-31T10:00:00Z", BillingCycle.Monthly, 1, "2026-02-28T10:00:00Z")]
    [InlineData("2026-01-31T10:00:00Z", BillingCycle.Monthly, 2, "2026-03-31T10:00:00Z")]
    [InlineData("2024-02-29T10:00:00Z", BillingCycle.Annual, 1, "2025-02-28T10:00:00Z")]
    [InlineData("2024-02-29T10:00:00Z", BillingCycle.Annual, 4, "2028-02-29T10:00:00Z")]
    public void Anchored_boundary_does_not_drift_after_short_months(
        string anchorText, BillingCycle cycle, int periodIndex, string expectedText)
    {
        var anchor = DateTimeOffset.Parse(anchorText);
        var expected = DateTimeOffset.Parse(expectedText);
        BillingPeriodService.Boundary(anchor, cycle, periodIndex).Should().Be(expected);
    }

    [Fact]
    public void Periods_are_half_open_at_the_renewal_instant()
    {
        var anchor = DateTimeOffset.Parse("2026-01-31T10:00:00Z");
        var due = DateTimeOffset.Parse("2026-02-28T10:00:00Z");
        BillingPeriodService.Containing(anchor, BillingCycle.Monthly, due.AddTicks(-1)).StartAt.Should().Be(anchor);
        var period = BillingPeriodService.Containing(anchor, BillingCycle.Monthly, due);
        period.StartAt.Should().Be(due);
        period.EndAt.Should().Be(DateTimeOffset.Parse("2026-03-31T10:00:00Z"));
    }

    [Fact]
    public void Verified_success_after_failure_settles_once_and_later_failure_cannot_reverse_it()
    {
        var intent = new PaymentIntent();
        var firstSettlement = DateTime.UtcNow;
        intent.MarkFailed();
        intent.MarkSucceeded(firstSettlement);
        intent.MarkSucceeded(firstSettlement.AddMinutes(1));
        intent.MarkFailed();
        intent.Status.Should().Be(PaymentIntentStatus.Succeeded);
        intent.SettledAt.Should().Be(firstSettlement);
        var attempt = new PaymentAttempt();
        attempt.MarkSucceeded(firstSettlement);
        attempt.MarkFailed(firstSettlement.AddMinutes(1), "delayed_failure");
        attempt.Status.Should().Be(PaymentAttemptStatus.Succeeded);
        attempt.SettledAt.Should().Be(firstSettlement);
    }

    [Fact]
    public void Failed_attempt_can_be_replaced_but_unknown_attempt_cannot()
    {
        var intent = new PaymentIntent();
        var failed = new PaymentAttempt { Status = PaymentAttemptStatus.Failed };
        intent.CanStartAttempt(new[] { failed }).Should().BeTrue();
        var unknown = new PaymentAttempt { Status = PaymentAttemptStatus.Unknown };
        intent.CanStartAttempt(new[] { failed, unknown }).Should().BeFalse();
    }

    [Fact]
    public void Waived_period_cannot_be_collected_later()
    {
        var period = new BillingPeriod();
        period.Waive(DateTime.UtcNow, "read-only missed period");
        var action = () => period.MarkPaid(DateTime.UtcNow);
        action.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Verified_renewal_settles_once_and_advances_to_the_anchored_period()
    {
        var tenantId = Guid.NewGuid();
        var subscription = new Subscription { TenantId = tenantId, Status = SubscriptionStatus.PastDue };
        var start = new DateTime(2026, 2, 28, 10, 0, 0, DateTimeKind.Utc);
        var period = new BillingPeriod
        {
            TenantId = tenantId, SubscriptionId = subscription.Id, StartAt = start,
            EndAt = new DateTime(2026, 3, 31, 10, 0, 0, DateTimeKind.Utc),
            Cycle = BillingCycle.Monthly, TotalAmount = 50m, Currency = "GHS",
        };
        var intent = new PaymentIntent
        {
            TenantId = tenantId, SubscriptionId = subscription.Id, BillingPeriodId = period.Id,
            Purpose = PaymentIntentPurpose.Renewal, TotalAmount = 50m, Currency = "GHS",
        };
        intent.MarkFailed();
        var attempt = new PaymentAttempt
        {
            TenantId = tenantId, PaymentIntentId = intent.Id, Reference = "renewal-reference",
            Amount = 50m, Currency = "GHS", Status = PaymentAttemptStatus.Unknown,
        };
        var confirmedAt = start.AddDays(2);
        SubscriptionStateMachine.SettleVerifiedPayment(subscription, period, intent, attempt, 50m, "GHS", confirmedAt)
            .Should().BeTrue();
        SubscriptionStateMachine.SettleVerifiedPayment(subscription, period, intent, attempt, 50m, "GHS", confirmedAt.AddMinutes(1))
            .Should().BeFalse();
        intent.SettledAt.Should().Be(confirmedAt);
        period.PaidAt.Should().Be(confirmedAt);
        subscription.CurrentPeriodStart.Should().Be(start);
        subscription.CurrentPeriodEnd.Should().Be(period.EndAt);
        subscription.Status.Should().Be(SubscriptionStatus.Active);
    }

    [Fact]
    public void Mismatched_provider_amount_never_changes_entitlements()
    {
        var tenantId = Guid.NewGuid();
        var subscription = new Subscription { TenantId = tenantId, Status = SubscriptionStatus.PastDue };
        var period = new BillingPeriod
        {
            TenantId = tenantId, SubscriptionId = subscription.Id,
            StartAt = DateTime.UtcNow, EndAt = DateTime.UtcNow.AddMonths(1), TotalAmount = 50m,
        };
        var intent = new PaymentIntent
        {
            TenantId = tenantId, SubscriptionId = subscription.Id, BillingPeriodId = period.Id,
            Purpose = PaymentIntentPurpose.Renewal, TotalAmount = 50m,
        };
        var attempt = new PaymentAttempt
        {
            TenantId = tenantId, PaymentIntentId = intent.Id, Reference = "wrong-amount", Amount = 50m,
        };
        FluentActions.Invoking(() => SubscriptionStateMachine.SettleVerifiedPayment(
                subscription, period, intent, attempt, 49m, "GHS", DateTime.UtcNow))
            .Should().Throw<InvalidOperationException>();
        intent.Status.Should().Be(PaymentIntentStatus.Pending);
        period.Status.Should().Be(BillingPeriodStatus.Pending);
        subscription.Status.Should().Be(SubscriptionStatus.PastDue);
    }

    [Fact]
    public void Paid_upgrade_preserves_existing_period_and_original_anchor()
    {
        var tenantId = Guid.NewGuid();
        var priorPlan = Guid.NewGuid();
        var upgradedPlan = Guid.NewGuid();
        var start = new DateTime(2026, 1, 31, 10, 0, 0, DateTimeKind.Utc);
        var end = new DateTime(2026, 2, 28, 10, 0, 0, DateTimeKind.Utc);
        var subscription = new Subscription
        {
            TenantId = tenantId, PlanDefinitionId = priorPlan, BillingAnchor = start,
            CurrentPeriodStart = start, CurrentPeriodEnd = end, Status = SubscriptionStatus.Active,
        };
        var period = new BillingPeriod
        {
            TenantId = tenantId, SubscriptionId = subscription.Id, StartAt = start,
            EndAt = end, TotalAmount = 50m,
        };
        period.MarkPaid(start);
        var intent = new PaymentIntent
        {
            TenantId = tenantId, SubscriptionId = subscription.Id, BillingPeriodId = period.Id,
            Purpose = PaymentIntentPurpose.Upgrade, TotalAmount = 20m,
            TargetPlanDefinitionId = upgradedPlan,
        };
        var attempt = new PaymentAttempt
        {
            TenantId = tenantId, PaymentIntentId = intent.Id, Reference = "upgrade-reference", Amount = 20m,
        };
        SubscriptionStateMachine.SettleVerifiedPayment(subscription, period, intent, attempt, 20m, "GHS", start.AddDays(15))
            .Should().BeTrue();
        subscription.PlanDefinitionId.Should().Be(upgradedPlan);
        subscription.BillingAnchor.Should().Be(start);
        subscription.CurrentPeriodEnd.Should().Be(end);
        period.PaidAt.Should().Be(start);
    }
}
