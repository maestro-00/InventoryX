using System.Diagnostics.Metrics;
using FluentAssertions;
using InventoryX.Infrastructure.Services;

namespace InventoryX.Infrastructure.Tests.BackgroundJobs;

public sealed class BillingObservabilityTests
{
    [Fact]
    public void Billing_meter_exposes_only_secret_free_operational_instruments()
    {
        using var listener = new MeterListener();
        var names = new List<string>();
        listener.InstrumentPublished = (instrument, _) =>
        {
            if (instrument.Meter.Name == BillingMetrics.Meter.Name)
                names.Add(instrument.Name);
        };
        listener.Start();

        BillingMetrics.RenewalAttempts.Add(1, new KeyValuePair<string, object?>("method", "Card"));
        BillingMetrics.WebhookDuplicates.Add(1);

        names.Should().Contain(new[] { "billing.renewal.attempts", "billing.webhooks.duplicates" });
        names.Should().NotContain(name => name.Contains("payload", StringComparison.OrdinalIgnoreCase));
    }
}
