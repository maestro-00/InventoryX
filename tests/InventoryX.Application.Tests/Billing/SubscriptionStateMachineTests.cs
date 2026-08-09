using FluentAssertions;
using InventoryX.Domain.Models.Tenancy;

namespace InventoryX.Application.Tests.Billing;

/// <summary>T073 - executable contract for the complete subscription lifecycle.</summary>
public sealed class SubscriptionStateMachineTests
{
    [Fact]
    public void State_machine_exposes_every_required_transition()
    {
        Enum.GetNames<SubscriptionStatus>().Should().BeEquivalentTo(
            "Trialing", "Active", "PastDue", "ReadOnly", "Cancelled", "PurgePending");
        var type = Type.GetType("InventoryX.Application.Services.SubscriptionStateMachine, InventoryX.Application");
        type.Should().NotBeNull();
        var required = new[]
        {
            "ExpireTrialToFree", "ActivatePaidPlan", "RecordChargeFailure", "RecordChargeSuccess",
            "ExhaustGrace", "CancelAtPeriodEnd", "Reactivate", "StartPurgeClock",
        };
        foreach (var method in required)
            type!.GetMethod(method).Should().NotBeNull($"{method} is a required lifecycle transition");
    }
}
