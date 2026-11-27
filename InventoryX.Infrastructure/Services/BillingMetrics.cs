using System.Diagnostics.Metrics;

namespace InventoryX.Infrastructure.Services;

/// <summary>Secret-free billing telemetry. Tags contain only stable state/outcome labels.</summary>
public static class BillingMetrics
{
    public static readonly Meter Meter = new("InventoryX.Billing", "1.0");
    public static readonly Counter<long> RenewalAttempts = Meter.CreateCounter<long>("billing.renewal.attempts");
    public static readonly Counter<long> RenewalFailures = Meter.CreateCounter<long>("billing.renewal.failures");
    public static readonly Counter<long> GraceLocks = Meter.CreateCounter<long>("billing.grace.locks");
    public static readonly Counter<long> WebhooksReceived = Meter.CreateCounter<long>("billing.webhooks.received");
    public static readonly Counter<long> WebhookDuplicates = Meter.CreateCounter<long>("billing.webhooks.duplicates");
    public static readonly Counter<long> WebhookMismatches = Meter.CreateCounter<long>("billing.webhooks.mismatches");
    public static readonly Counter<long> RetentionPostponements = Meter.CreateCounter<long>("billing.retention.postponements");
}
