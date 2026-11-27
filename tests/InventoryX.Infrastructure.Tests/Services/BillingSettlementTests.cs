using FluentAssertions;
using InventoryX.Application.Services.IServices;
using InventoryX.Domain.Models.Tenancy;
using InventoryX.Infrastructure.Data;
using InventoryX.Infrastructure.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace InventoryX.Infrastructure.Tests.Services;

public sealed class BillingSettlementTests
{
    [Fact]
    public async Task Verified_replacement_retires_old_card_only_after_new_method_is_confirmed()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:;Foreign Keys=False");
        await connection.OpenAsync();
        var tenant = new TestTenantContext { TenantId = Guid.NewGuid() };
        await using var context = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection).Options, tenant);
        await context.Database.EnsureCreatedAsync();
        var old = new PaymentMethod { TenantId = tenant.TenantId!.Value,
            Kind = PaymentMethodKind.Card, RenewalMode = PaymentRenewalMode.Automatic,
            Provider = "paystack", MaskedIdentifier = "**** 1111", EncryptedAuthorizationCode = "v1:old" };
        old.Activate(DateTime.UtcNow.AddDays(-10));
        var pending = new PaymentMethod { TenantId = tenant.TenantId.Value,
            Kind = PaymentMethodKind.Card, RenewalMode = PaymentRenewalMode.Automatic,
            Provider = "paystack", MaskedIdentifier = "Card awaiting verification" };
        var subscription = new Subscription { TenantId = tenant.TenantId.Value,
            CurrentPaymentMethodId = old.Id, Status = SubscriptionStatus.Active };
        var intent = new PaymentIntent { TenantId = tenant.TenantId.Value,
            SubscriptionId = subscription.Id, Purpose = PaymentIntentPurpose.MethodVerification,
            TotalAmount = 1m, IdempotencyKey = "replace-1" };
        var attempt = new PaymentAttempt { TenantId = tenant.TenantId.Value,
            PaymentIntentId = intent.Id, PaymentMethodId = pending.Id,
            Reference = "replacement-ref", Amount = 1m };
        context.AddRange(new Tenant { Id = tenant.TenantId.Value, Name = "Replacement Shop" },
            subscription, old, pending, intent, attempt);
        await context.SaveChangesAsync();

        var service = new BillingSettlementService(context, tenant, new FakeProtector());
        (await service.SettleAsync(attempt.Reference, 1m, "GHS", DateTime.UtcNow,
            authorizationCode: "AUTH-new", cardLast4: "4242")).Should().BeTrue();

        old.Status.Should().Be(PaymentMethodStatus.Retired);
        pending.Status.Should().Be(PaymentMethodStatus.Active);
        pending.MaskedIdentifier.Should().Be("**** 4242");
        subscription.CurrentPaymentMethodId.Should().Be(pending.Id);
        (await context.BillingInvoices.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Initial_card_settlement_activates_only_verified_reusable_authorization()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:;Foreign Keys=False");
        await connection.OpenAsync();
        var tenant = new TestTenantContext { TenantId = Guid.NewGuid() };
        await using var context = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection).Options, tenant);
        await context.Database.EnsureCreatedAsync();
        var subscription = new Subscription { TenantId = tenant.TenantId!.Value, Status = SubscriptionStatus.Trialing };
        var period = new BillingPeriod { TenantId = subscription.TenantId, SubscriptionId = subscription.Id,
            StartAt = DateTime.UtcNow, EndAt = DateTime.UtcNow.AddMonths(1), TotalAmount = 30m };
        var intent = new PaymentIntent { TenantId = subscription.TenantId, SubscriptionId = subscription.Id,
            BillingPeriodId = period.Id, Purpose = PaymentIntentPurpose.Initial,
            TargetPlanDefinitionId = Guid.NewGuid(), TargetCycle = BillingCycle.Monthly,
            TotalAmount = 30m, IdempotencyKey = "initial-card" };
        var method = new PaymentMethod { TenantId = subscription.TenantId, Kind = PaymentMethodKind.Card,
            RenewalMode = PaymentRenewalMode.Automatic, Provider = "paystack", MaskedIdentifier = "Card" };
        var attempt = new PaymentAttempt { TenantId = subscription.TenantId, PaymentIntentId = intent.Id,
            PaymentMethodId = method.Id, Reference = "card-ref", Amount = 30m };
        context.AddRange(new Tenant { Id = subscription.TenantId, Name = "Card Shop",
            BillingEmail = "owner@example.com" }, subscription, period, intent, method, attempt);
        await context.SaveChangesAsync();
        var protector = new FakeProtector();
        var service = new BillingSettlementService(context, tenant, protector);

        (await service.SettleAsync(attempt.Reference, 30m, "GHS", DateTime.UtcNow,
            authorizationCode: "AUTH_reusable", cardBrand: "Visa", cardLast4: "4242",
            cardExpiryMonth: 12, cardExpiryYear: 2028)).Should().BeTrue();

        method.Status.Should().Be(PaymentMethodStatus.Active);
        protector.Unprotect(subscription.TenantId, method.EncryptedAuthorizationCode!).Should().Be("AUTH_reusable");
        subscription.CurrentPaymentMethodId.Should().Be(method.Id);
        method.MaskedIdentifier.Should().Be("**** 4242");
        method.Brand.Should().Be("Visa");
        method.ExpiryYear.Should().Be(2028);
        (await context.BillingInvoices.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Verified_payment_commits_one_invoice_and_entitlement_for_the_current_tenant()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:;Foreign Keys=False");
        await connection.OpenAsync();
        var tenant = new TestTenantContext { TenantId = Guid.NewGuid() };
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
        await using var context = new AppDbContext(options, tenant);
        await context.Database.EnsureCreatedAsync();
        var start = new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc);
        var subscription = new Subscription { TenantId = tenant.TenantId!.Value, Status = SubscriptionStatus.PastDue };
        var period = new BillingPeriod
        {
            TenantId = subscription.TenantId, SubscriptionId = subscription.Id, StartAt = start,
            EndAt = start.AddMonths(1), TotalAmount = 30m, BaseAmount = 25m, TaxAmount = 5m,
        };
        var intent = new PaymentIntent
        {
            TenantId = subscription.TenantId, SubscriptionId = subscription.Id, BillingPeriodId = period.Id,
            Purpose = PaymentIntentPurpose.Renewal, TotalAmount = 30m, BaseAmount = 25m, TaxAmount = 5m,
            IdempotencyKey = "renewal-1",
        };
        var attempt = new PaymentAttempt
        {
            TenantId = subscription.TenantId, PaymentIntentId = intent.Id, Reference = "pay-1", Amount = 30m,
        };
        context.AddRange(new Tenant { Id = subscription.TenantId, Name = "Renewal Shop",
            BillingEmail = "owner@example.com" }, subscription, period, intent, attempt);
        await context.SaveChangesAsync();

        var service = new BillingSettlementService(context, tenant);
        var confirmedAt = start.AddDays(2);
        await FluentActions.Invoking(() => service.SettleAsync(attempt.Reference, 29m, "GHS", confirmedAt))
            .Should().ThrowAsync<InvalidOperationException>();
        context.ChangeTracker.Clear();
        (await context.BillingInvoices.CountAsync()).Should().Be(0);
        (await context.Subscriptions.SingleAsync()).Status.Should().Be(SubscriptionStatus.PastDue);
        (await service.SettleAsync(attempt.Reference, 30m, "GHS", confirmedAt)).Should().BeTrue();
        (await service.SettleAsync(attempt.Reference, 30m, "GHS", confirmedAt)).Should().BeFalse();
        context.ChangeTracker.Clear();
        var invoice = await context.BillingInvoices.SingleAsync();
        invoice.Amount.Should().Be(30m);
        invoice.TaxAmount.Should().Be(5m);
        var pdf = System.Text.Encoding.ASCII.GetString(Convert.FromBase64String(invoice.PdfContent!));
        pdf.Should().StartWith("%PDF-1.4").And.Contain("xref").And.Contain("%%EOF")
            .And.Contain("Renewal Shop").And.Contain("Paid total: 30.00 GHS");
        (await context.Subscriptions.SingleAsync()).Status.Should().Be(SubscriptionStatus.Active);
        (await context.PaymentIntents.SingleAsync()).Status.Should().Be(PaymentIntentStatus.Succeeded);

        tenant.TenantId = Guid.NewGuid();
        (await service.SettleAsync(attempt.Reference, 30m, "GHS", confirmedAt)).Should().BeFalse();
    }

    private sealed class TestTenantContext : ITenantContext
    {
        public Guid? TenantId { get; set; }
        public string? UserId { get; set; }
        public string? Role { get; set; }
        public string? LocationScope { get; set; }
    }

    private sealed class FakeProtector : IBillingSecretProtector
    {
        public string Protect(Guid tenantId, string secret) => "v1:" + secret;
        public string Unprotect(Guid tenantId, string ciphertext) => ciphertext[3..];
    }
}
