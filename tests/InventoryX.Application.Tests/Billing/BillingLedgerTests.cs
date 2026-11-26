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
}
