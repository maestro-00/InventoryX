using InventoryX.Domain.Models.Tenancy;

namespace InventoryX.Application.Services;

/// <summary>Calculates half-open billing periods from the original calendar anchor.</summary>
public static class BillingPeriodService
{
    public static DateTimeOffset Boundary(DateTimeOffset anchor, BillingCycle cycle, int periodIndex)
    {
        if (periodIndex < 0) throw new ArgumentOutOfRangeException(nameof(periodIndex));
        var utcAnchor = anchor.ToUniversalTime();
        return cycle switch
        {
            BillingCycle.Monthly => utcAnchor.AddMonths(periodIndex),
            BillingCycle.Annual => utcAnchor.AddYears(periodIndex),
            _ => throw new ArgumentOutOfRangeException(nameof(cycle)),
        };
    }

    public static (DateTimeOffset StartAt, DateTimeOffset EndAt) Containing(
        DateTimeOffset anchor, BillingCycle cycle, DateTimeOffset instant)
    {
        anchor = anchor.ToUniversalTime();
        instant = instant.ToUniversalTime();
        if (instant < anchor) throw new ArgumentOutOfRangeException(nameof(instant));

        var index = cycle switch
        {
            BillingCycle.Monthly => (instant.Year - anchor.Year) * 12 + instant.Month - anchor.Month,
            BillingCycle.Annual => instant.Year - anchor.Year,
            _ => throw new ArgumentOutOfRangeException(nameof(cycle)),
        };
        while (Boundary(anchor, cycle, index) > instant) index--;
        while (Boundary(anchor, cycle, index + 1) <= instant) index++;
        return (Boundary(anchor, cycle, index), Boundary(anchor, cycle, index + 1));
    }
}
