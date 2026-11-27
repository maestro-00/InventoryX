using FluentAssertions;
using System.Security.Cryptography;
using System.Text;
using InventoryX.Application.Options;
using InventoryX.Application.Services.IServices;
using InventoryX.Domain.Models.Tenancy;
using InventoryX.Infrastructure.Data;
using InventoryX.Infrastructure.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace InventoryX.Infrastructure.Tests.Services;

/// <summary>T075 - webhook authenticity and exactly-once event processing contract.</summary>
public sealed class PaystackWebhookTests
{
    [Fact]
    public async Task Ingress_persists_signed_event_before_provider_verification_and_worker_settles_it()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:;Foreign Keys=False");
        await connection.OpenAsync();
        var tenantContext = new TestTenantContext();
        await using var context = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection).Options, tenantContext);
        await context.Database.EnsureCreatedAsync();
        var tenantId = Guid.NewGuid();
        var subscription = new Subscription { TenantId = tenantId, Status = SubscriptionStatus.Trialing };
        var period = new BillingPeriod { TenantId = tenantId, SubscriptionId = subscription.Id,
            StartAt = DateTime.UtcNow, EndAt = DateTime.UtcNow.AddMonths(1), TotalAmount = 30m };
        var intent = new PaymentIntent { TenantId = tenantId, SubscriptionId = subscription.Id,
            BillingPeriodId = period.Id, Purpose = PaymentIntentPurpose.Initial, TotalAmount = 30m,
            TargetPlanDefinitionId = Guid.NewGuid(), TargetCycle = BillingCycle.Monthly,
            IdempotencyKey = "webhook-async" };
        var method = new PaymentMethod { TenantId = tenantId, Kind = PaymentMethodKind.Card,
            RenewalMode = PaymentRenewalMode.Automatic, Provider = "paystack",
            MaskedIdentifier = "Card awaiting verification" };
        var attempt = new PaymentAttempt { TenantId = tenantId, PaymentIntentId = intent.Id,
            PaymentMethodId = method.Id, Reference = "webhook-async-reference", Amount = 30m };
        context.AddRange(new Tenant { Id = tenantId, Name = "Webhook Shop" },
            subscription, period, intent, method, attempt);
        await context.SaveChangesAsync();
        var gateway = new FakePaymentGateway { Result = new PaymentVerificationResult(
            attempt.Reference, "success", 30m, "GHS", null, DateTime.UtcNow) };
        var processor = new PaystackWebhookProcessor(context,
            Options.Create(new PaystackOptions { SecretKey = "sk_test" }), new FakeInvoiceService(),
            gateway, tenantContext, new BillingSettlementService(context, tenantContext));
        const string payload = """{"id":"async-event","event":"charge.success","data":{"reference":"webhook-async-reference"}}""";

        (await processor.EnqueueAsync(payload, PaystackSignatureMode.LegacySecret)).AlreadyProcessed.Should().BeFalse();
        gateway.Verifications.Should().Be(0);
        (await context.PaystackEventInbox.IgnoreQueryFilters().SingleAsync()).Status.Should().Be(PaystackEventStatus.Received);
        (await context.PaymentIntents.IgnoreQueryFilters().SingleAsync()).Status.Should().Be(PaymentIntentStatus.Pending);

        context.PaystackEventInbox.AddRange(Enumerable.Range(0, 51).Select(index => new PaystackEventInbox
        {
            ProviderEventId = $"unknown-{index}", PayloadHash = $"unknown-{index}",
            Reference = $"unknown-{index}", EventType = "charge.success",
            ReceivedAt = DateTime.UtcNow.AddMinutes(-1),
        }));
        await context.SaveChangesAsync();

        await processor.ReconcilePendingAsync();
        gateway.Verifications.Should().Be(1);
        (await context.PaystackEventInbox.IgnoreQueryFilters().SingleAsync(item => item.ProviderEventId == "async-event"))
            .Status.Should().Be(PaystackEventStatus.Processed);
        (await context.PaymentIntents.IgnoreQueryFilters().SingleAsync()).Status.Should().Be(PaymentIntentStatus.Succeeded);
        (await context.BillingInvoices.IgnoreQueryFilters().CountAsync()).Should().Be(1);
        method.Status.Should().Be(PaymentMethodStatus.Invalid);
        subscription.CurrentPaymentMethodId.Should().BeNull();
        (await processor.EnqueueAsync(payload, PaystackSignatureMode.LegacySecret)).AlreadyProcessed.Should().BeTrue();
    }

    [Fact]
    public async Task Distinct_events_without_root_ids_are_not_collapsed_by_reference()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using var context = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection).Options, new TestTenantContext());
        await context.Database.EnsureCreatedAsync();
        var processor = new PaystackWebhookProcessor(context,
            Options.Create(new PaystackOptions { SecretKey = "sk_test" }), new FakeInvoiceService());
        const string first = """{"event":"charge.success","data":{"id":123,"reference":"same-reference"}}""";
        const string second = """{"event":"charge.success","data":{"id":124,"reference":"same-reference"}}""";

        (await processor.EnqueueAsync(first, PaystackSignatureMode.LegacySecret)).AlreadyProcessed.Should().BeFalse();
        (await processor.EnqueueAsync(second, PaystackSignatureMode.LegacySecret)).AlreadyProcessed.Should().BeFalse();
        (await processor.EnqueueAsync(first, PaystackSignatureMode.LegacySecret)).AlreadyProcessed.Should().BeTrue();
        (await context.PaystackEventInbox.IgnoreQueryFilters().CountAsync()).Should().Be(2);
    }

    [Fact]
    public void Timestamped_signature_accepts_any_valid_v1_and_never_falls_back_when_present()
    {
        using var context = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite("DataSource=:memory:").Options);
        var processor = new PaystackWebhookProcessor(context,
            Options.Create(new PaystackOptions { SecretKey = "sk_test", WebhookSigningSecret = "whsec_test" }),
            new FakeInvoiceService());
        var body = Encoding.UTF8.GetBytes("""{"event":"charge.success","data":{"reference":"ref-1"}}""");
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var signed = Encoding.UTF8.GetBytes($"{timestamp}.{Encoding.UTF8.GetString(body)}");
        var digest = Convert.ToHexString(HMACSHA512.HashData(Encoding.UTF8.GetBytes("whsec_test"), signed))
            .ToLowerInvariant();
        var legacy = Convert.ToHexString(HMACSHA512.HashData(Encoding.UTF8.GetBytes("sk_test"), body));

        processor.VerifySignature(body, $"t={timestamp},v1=whk_bad:{new string('0', 128)},v1=whk_good:{digest}", legacy)
            .Should().Be(PaystackSignatureMode.SigningSecret);
        processor.VerifySignature(body, $"t={timestamp - 301},v1=whk_good:{digest}", legacy).Should().BeNull();
        processor.VerifySignature(body, "malformed", legacy).Should().BeNull();
        processor.VerifySignature(body, null, legacy).Should().Be(PaystackSignatureMode.LegacySecret);
    }
    [Fact]
    public void Legacy_signature_matches_raw_payload_and_rejects_tampering()
    {
        using var context = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite("DataSource=:memory:").Options);
        var processor = new PaystackWebhookProcessor(context,
            Options.Create(new PaystackOptions { SecretKey = "sk_test" }), new FakeInvoiceService());
        const string payload = """{"event":"charge.success","data":{"reference":"ref-1"}}""";
        var signature = Convert.ToHexString(HMACSHA512.HashData(
            Encoding.UTF8.GetBytes("sk_test"), Encoding.UTF8.GetBytes(payload)));

        processor.VerifySignature(payload, signature).Should().BeTrue();
        processor.VerifySignature(payload + " ", signature).Should().BeFalse();
        processor.VerifySignature(payload, "invalid").Should().BeFalse();
    }

    [Fact]
    public async Task Genuine_delayed_payment_event_is_accepted_and_duplicate_is_safe()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        var tenantContext = new TestTenantContext();
        await using var context = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection).Options, tenantContext);
        await context.Database.EnsureCreatedAsync();
        var processor = new PaystackWebhookProcessor(context,
            Options.Create(new PaystackOptions { SecretKey = "sk_test" }), new FakeInvoiceService());
        var payload = """{"id":"old-event","event":"charge.success","data":{"reference":"unknown-reference","amount":3000,"currency":"GHS","paid_at":"2025-01-01T00:00:00Z"}}""";

        (await processor.ProcessAsync(payload)).AlreadyProcessed.Should().BeFalse();
        (await processor.ProcessAsync(payload)).AlreadyProcessed.Should().BeTrue();
        (await context.PaystackEventInbox.IgnoreQueryFilters().CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Known_reference_settles_only_the_matching_verified_amount_and_currency()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:;Foreign Keys=False");
        await connection.OpenAsync();
        var tenantContext = new TestTenantContext();
        await using var context = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection).Options, tenantContext);
        await context.Database.EnsureCreatedAsync();
        var tenantId = Guid.NewGuid();
        var start = new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc);
        var subscription = new Subscription { TenantId = tenantId, Status = SubscriptionStatus.Trialing };
        var period = new BillingPeriod
        {
            TenantId = tenantId, SubscriptionId = subscription.Id, StartAt = start,
            EndAt = start.AddMonths(1), TotalAmount = 30m,
        };
        var intent = new PaymentIntent
        {
            TenantId = tenantId, SubscriptionId = subscription.Id, BillingPeriodId = period.Id,
            Purpose = PaymentIntentPurpose.Initial, TotalAmount = 30m,
            TargetPlanDefinitionId = Guid.NewGuid(), TargetCycle = BillingCycle.Monthly,
            IdempotencyKey = "initial-1",
        };
        var attempt = new PaymentAttempt
        {
            TenantId = tenantId, PaymentIntentId = intent.Id, Reference = "initial-reference", Amount = 30m,
        };
        context.AddRange(new Tenant { Id = tenantId, Name = "Known Reference Shop" },
            subscription, period, intent, attempt);
        await context.SaveChangesAsync();
        var gateway = new FakePaymentGateway { Result = new PaymentVerificationResult(
            "initial-reference", "success", 30m, "GHS", null, DateTime.UtcNow) };
        var processor = new PaystackWebhookProcessor(context,
            Options.Create(new PaystackOptions { SecretKey = "sk_test" }), new FakeInvoiceService(),
            gateway, tenantContext, new BillingSettlementService(context, tenantContext));

        gateway.Result = gateway.Result with { Status = "pending" };
        await processor.ProcessAsync("""{"id":"pending-event","event":"charge.success","data":{"reference":"initial-reference","amount":3000,"currency":"GHS"}}""");
        (await context.PaymentIntents.IgnoreQueryFilters().SingleAsync()).Status.Should().Be(PaymentIntentStatus.Pending);
        gateway.Result = gateway.Result with { Status = "success", Currency = "NGN" };
        await processor.ProcessAsync("""{"id":"wrong-currency","event":"charge.success","data":{"reference":"initial-reference","amount":3000,"currency":"GHS"}}""");
        (await context.PaymentIntents.IgnoreQueryFilters().SingleAsync()).Status.Should().Be(PaymentIntentStatus.Pending);
        gateway.Result = gateway.Result with { Amount = 29m };
        await processor.ProcessAsync("""{"id":"wrong-amount","event":"charge.success","data":{"reference":"initial-reference","amount":2900,"currency":"GHS"}}""");
        (await context.PaymentIntents.IgnoreQueryFilters().SingleAsync()).Status.Should().Be(PaymentIntentStatus.Pending);
        gateway.Result = gateway.Result with { Amount = 30m, Currency = "GHS" };
        await processor.ProcessAsync("""{"id":"right-amount","event":"charge.success","data":{"reference":"initial-reference","amount":3000,"currency":"GHS"}}""");
        (await context.PaymentIntents.IgnoreQueryFilters().SingleAsync()).Status.Should().Be(PaymentIntentStatus.Succeeded);
        (await context.BillingInvoices.IgnoreQueryFilters().CountAsync()).Should().Be(1);
    }

    private sealed class FakeInvoiceService : IBillingInvoiceService
    {
        public Task<BillingInvoice> GenerateAndEmailAsync(Subscription subscription, string paymentReference,
            CancellationToken cancellationToken = default) => throw new InvalidOperationException("Legacy invoice path must not run.");
    }

    private sealed class FakePaymentGateway : IPaymentGateway
    {
        public PaymentVerificationResult Result { get; set; } = new("", "pending", 0, "GHS", null, null);
        public int Verifications { get; private set; }
        public Task<PaymentVerificationResult> VerifyAsync(string reference, CancellationToken cancellationToken = default)
        {
            Verifications++;
            return Task.FromResult(Result);
        }
        public Task<PaymentInitializationResult> InitializeAuthorizationAsync(PaymentInitializationRequest request,
            CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<PaymentChargeResult> ChargeAsync(PaymentChargeRequest request,
            CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    private sealed class TestTenantContext : ITenantContext
    {
        public Guid? TenantId { get; set; }
        public string? UserId { get; set; }
        public string? Role { get; set; }
        public string? LocationScope { get; set; }
    }

    [Fact]
    public void Webhook_processor_exposes_signature_verification_and_idempotent_processing()
    {
        var processor = Type.GetType("InventoryX.Infrastructure.Services.PaystackWebhookProcessor, InventoryX.Infrastructure");
        processor.Should().NotBeNull();
        processor!.GetMethods().Should().Contain(method => method.Name == "VerifySignature",
            "Paystack's x-paystack-signature HMAC-SHA512 must be verified before processing");
        processor.GetMethods().Should().Contain(method => method.Name == "ProcessAsync",
            "event IDs must be persisted so duplicate deliveries become no-ops");

        Type.GetType("InventoryX.Domain.Models.Tenancy.ProcessedWebhookEvent, InventoryX.Domain")
            .Should().NotBeNull("webhook event IDs require durable idempotency storage");
    }
}
