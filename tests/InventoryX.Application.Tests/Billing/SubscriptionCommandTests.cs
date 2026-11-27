using FluentAssertions;
using InventoryX.Application.Commands.RequestHandlers.Billing;
using InventoryX.Application.Commands.Requests.Billing;
using InventoryX.Common.Tests;
using InventoryX.Application.Services.IServices;
using InventoryX.Domain.Models.Catalog;
using InventoryX.Domain.Models.Tenancy;
using InventoryX.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace InventoryX.Application.Tests.Billing;

public sealed class SubscriptionCommandTests : IDisposable
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly TestDb _db;

    public SubscriptionCommandTests() => _db = new TestDb(_tenantId, "owner-1");

    [Fact]
    public async Task Initial_quote_snapshots_published_price_tax_and_expiry()
    {
        await using var context = _db.CreateContext();
        var free = new PlanDefinition { Name = "Free", Tier = PlanTier.Free };
        var paid = new PlanDefinition { Name = "Standard", Tier = PlanTier.Standard,
            MonthlyPrice = 199m, AnnualPrice = 1990m };
        context.Tenants.Add(new Tenant { Id = _tenantId, Name = "Quote shop", BillingEmail = "owner@example.com" });
        context.PlanDefinitions.AddRange(free, paid);
        context.Subscriptions.Add(new Subscription { TenantId = _tenantId, PlanDefinitionId = free.Id,
            Status = SubscriptionStatus.Active });
        await context.SaveChangesAsync();
        _db.TenantContext.Role = "Owner";

        var handler = new QuoteBillingCommandHandler(context, _db.TenantContext);
        var first = await handler.Handle(new QuoteBillingCommand(paid.Id, BillingCycle.Monthly,
            PaymentIntentPurpose.Initial), CancellationToken.None);
        first.BaseAmount.Should().Be(199m);
        first.TotalAmount.Should().Be(first.BaseAmount + first.TaxAmount);
        first.Currency.Should().Be("GHS");
        first.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
        first.NextRenewalAt.Should().BeAfter(first.EffectiveAt);

        paid.MonthlyPrice = 249m;
        await context.SaveChangesAsync();
        (await context.BillingQuotes.SingleAsync(q => q.Id == first.QuoteId)).BaseAmount.Should().Be(199m);
        var second = await handler.Handle(new QuoteBillingCommand(paid.Id, BillingCycle.Monthly,
            PaymentIntentPurpose.Initial), CancellationToken.None);
        second.BaseAmount.Should().Be(249m);
    }

    [Fact]
    public async Task Quote_requires_owner_and_a_published_self_service_plan()
    {
        await using var context = _db.CreateContext();
        var enterprise = new PlanDefinition { Name = "Enterprise", Tier = PlanTier.Enterprise,
            MonthlyPrice = 1299m };
        context.Tenants.Add(new Tenant { Id = _tenantId, Name = "Quote shop" });
        context.PlanDefinitions.Add(enterprise);
        context.Subscriptions.Add(new Subscription { TenantId = _tenantId, PlanDefinitionId = enterprise.Id });
        await context.SaveChangesAsync();
        var handler = new QuoteBillingCommandHandler(context, _db.TenantContext);
        _db.TenantContext.Role = "Manager";
        await FluentActions.Invoking(() => handler.Handle(new QuoteBillingCommand(enterprise.Id,
            BillingCycle.Monthly, PaymentIntentPurpose.Initial), CancellationToken.None))
            .Should().ThrowAsync<UnauthorizedAccessException>();

        _db.TenantContext.Role = "Owner";
        await FluentActions.Invoking(() => handler.Handle(new QuoteBillingCommand(enterprise.Id,
            BillingCycle.Monthly, PaymentIntentPurpose.Initial), CancellationToken.None))
            .Should().ThrowAsync<InventoryX.Application.Exceptions.ConflictException>();
    }

    [Fact]
    public async Task Pending_card_purchase_keeps_old_plan_and_repeated_confirmation_reuses_one_intent()
    {
        await using var context = _db.CreateContext();
        var (free, paid, subscription) = await SeedPurchaseAsync(context);
        var quote = await new QuoteBillingCommandHandler(context, _db.TenantContext)
            .Handle(new QuoteBillingCommand(paid.Id, BillingCycle.Monthly, PaymentIntentPurpose.Initial), CancellationToken.None);
        var gateway = new FakePaymentGateway();
        var handler = new CreatePaymentIntentCommandHandler(context, _db.TenantContext, gateway,
            Microsoft.Extensions.Options.Options.Create(new InventoryX.Application.Options.PaystackOptions
            { CallbackUrl = "https://billing.example/settings/billing" }), new FakeProtector());
        var command = new CreatePaymentIntentCommand(quote.QuoteId, "card", IdempotencyKey: "purchase-1");

        var first = await handler.Handle(command, CancellationToken.None);
        var second = await handler.Handle(command, CancellationToken.None);
        first.IntentId.Should().Be(second.IntentId);
        first.Status.Should().Be("pending");
        first.AuthorizationUrl.Should().Be("https://checkout.example/card");
        gateway.InitializationCalls.Should().Be(1);
        gateway.LastInitializationRequest!.CallbackUrl.Should().Be("https://billing.example/settings/billing");
        (await context.Subscriptions.SingleAsync()).PlanDefinitionId.Should().Be(free.Id);
        (await context.BillingPeriods.CountAsync()).Should().Be(1);
        (await context.PaymentAttempts.CountAsync()).Should().Be(1);
        (await context.PaymentMethods.CountAsync(method => method.Status == PaymentMethodStatus.Pending)).Should().Be(1);

        var reference = (await context.PaymentAttempts.SingleAsync()).Reference;
        (await new BillingSettlementService(context, _db.TenantContext)
            .SettleAsync(reference, quote.TotalAmount, "GHS", DateTime.UtcNow)).Should().BeTrue();
        (await context.Subscriptions.SingleAsync()).PlanDefinitionId.Should().Be(paid.Id);
        (await context.BillingInvoices.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Purchase_rejects_expired_and_repriced_quotes_without_charging()
    {
        await using var context = _db.CreateContext();
        var (_, paid, _) = await SeedPurchaseAsync(context);
        var quoteHandler = new QuoteBillingCommandHandler(context, _db.TenantContext);
        var first = await quoteHandler.Handle(new QuoteBillingCommand(paid.Id, BillingCycle.Monthly,
            PaymentIntentPurpose.Initial), CancellationToken.None);
        var gateway = new FakePaymentGateway();
        var handler = new CreatePaymentIntentCommandHandler(context, _db.TenantContext, gateway,
            Microsoft.Extensions.Options.Options.Create(new InventoryX.Application.Options.PaystackOptions
            { CallbackUrl = "https://billing.example/settings/billing" }), new FakeProtector());
        var expired = await context.BillingQuotes.SingleAsync(q => q.Id == first.QuoteId);
        expired.ExpiresAt = DateTime.UtcNow.AddSeconds(-1);
        await context.SaveChangesAsync();
        await FluentActions.Invoking(() => handler.Handle(new CreatePaymentIntentCommand(first.QuoteId,
            "card", IdempotencyKey: "expired"), CancellationToken.None))
            .Should().ThrowAsync<InventoryX.Application.Exceptions.ConflictException>();

        var second = await quoteHandler.Handle(new QuoteBillingCommand(paid.Id, BillingCycle.Monthly,
            PaymentIntentPurpose.Initial), CancellationToken.None);
        paid.MonthlyPrice += 10m;
        await context.SaveChangesAsync();
        await FluentActions.Invoking(() => handler.Handle(new CreatePaymentIntentCommand(second.QuoteId,
            "card", IdempotencyKey: "repriced"), CancellationToken.None))
            .Should().ThrowAsync<InventoryX.Application.Exceptions.ConflictException>();
        gateway.InitializationCalls.Should().Be(0);
    }

    [Theory]
    [InlineData("pay_offline", "awaiting_approval")]
    [InlineData("failed", "failed")]
    public async Task Mobile_money_pending_or_failed_never_activates_the_paid_plan(
        string providerStatus, string expectedStatus)
    {
        await using var context = _db.CreateContext();
        var (free, paid, _) = await SeedPurchaseAsync(context);
        var quote = await new QuoteBillingCommandHandler(context, _db.TenantContext)
            .Handle(new QuoteBillingCommand(paid.Id, BillingCycle.Monthly, PaymentIntentPurpose.Initial), CancellationToken.None);
        var gateway = new FakePaymentGateway { ChargeStatus = providerStatus };
        var result = await new CreatePaymentIntentCommandHandler(context, _db.TenantContext, gateway,
                Microsoft.Extensions.Options.Options.Create(new InventoryX.Application.Options.PaystackOptions
                { CallbackUrl = "https://billing.example/settings/billing" }), new FakeProtector())
            .Handle(new CreatePaymentIntentCommand(quote.QuoteId, "mobile_money", "mtn",
                "0241234567", $"momo-{providerStatus}"), CancellationToken.None);

        result.Status.Should().Be(expectedStatus);
        result.AuthorizationUrl.Should().BeNull();
        gateway.ChargeCalls.Should().Be(1);
        (await context.Subscriptions.SingleAsync()).PlanDefinitionId.Should().Be(free.Id);
        (await context.BillingInvoices.CountAsync()).Should().Be(0);
        var method = await context.PaymentMethods.SingleAsync();
        method.Kind.Should().Be(PaymentMethodKind.MobileMoney);
        method.RenewalMode.Should().Be(PaymentRenewalMode.OwnerApproval);
        method.EncryptedPhoneNumber.Should().StartWith("v1:");
        method.EncryptedAuthorizationCode.Should().BeNull();
        method.Status.Should().Be(providerStatus == "failed" ? PaymentMethodStatus.Invalid : PaymentMethodStatus.Pending);
        (await context.Subscriptions.SingleAsync()).CurrentPaymentMethodId.Should().BeNull();
    }

    [Fact]
    public async Task Unknown_provider_outcome_keeps_one_attempt_for_later_verification()
    {
        await using var context = _db.CreateContext();
        var (free, paid, _) = await SeedPurchaseAsync(context);
        var quote = await new QuoteBillingCommandHandler(context, _db.TenantContext)
            .Handle(new QuoteBillingCommand(paid.Id, BillingCycle.Monthly, PaymentIntentPurpose.Initial), CancellationToken.None);
        var gateway = new FakePaymentGateway { ThrowOnInitialize = true };
        var handler = new CreatePaymentIntentCommandHandler(context, _db.TenantContext, gateway,
            Microsoft.Extensions.Options.Options.Create(new InventoryX.Application.Options.PaystackOptions
            { CallbackUrl = "https://billing.example/settings/billing" }), new FakeProtector());
        var command = new CreatePaymentIntentCommand(quote.QuoteId, "card", IdempotencyKey: "unknown-1");

        await FluentActions.Invoking(() => handler.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<TimeoutException>();
        (await context.PaymentAttempts.SingleAsync()).Status.Should().Be(PaymentAttemptStatus.Unknown);
        (await handler.Handle(command, CancellationToken.None)).Status.Should().Be("pending");
        gateway.InitializationCalls.Should().Be(1);
        (await context.Subscriptions.SingleAsync()).PlanDefinitionId.Should().Be(free.Id);
    }

    [Fact]
    public async Task Card_checkout_rejects_missing_return_url_before_creating_an_attempt()
    {
        await using var context = _db.CreateContext();
        var (_, paid, _) = await SeedPurchaseAsync(context);
        var quote = await new QuoteBillingCommandHandler(context, _db.TenantContext)
            .Handle(new QuoteBillingCommand(paid.Id, BillingCycle.Monthly, PaymentIntentPurpose.Initial), CancellationToken.None);
        var gateway = new FakePaymentGateway();
        var handler = new CreatePaymentIntentCommandHandler(context, _db.TenantContext, gateway,
            Microsoft.Extensions.Options.Options.Create(new InventoryX.Application.Options.PaystackOptions()),
            new FakeProtector());

        await FluentActions.Invoking(() => handler.Handle(new CreatePaymentIntentCommand(
                quote.QuoteId, "card", IdempotencyKey: "missing-callback"), CancellationToken.None))
            .Should().ThrowAsync<FluentValidation.ValidationException>();
        gateway.InitializationCalls.Should().Be(0);
        (await context.PaymentAttempts.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Recovery_quote_with_recovery_price_marker_can_start_payment()
    {
        await using var context = _db.CreateContext();
        var (_, paid, subscription) = await SeedPurchaseAsync(context);
        subscription.Status = SubscriptionStatus.ReadOnly;
        subscription.CurrentPeriodStart = DateTime.UtcNow.AddDays(-10);
        subscription.CurrentPeriodEnd = DateTime.UtcNow.AddDays(20);
        await context.SaveChangesAsync();
        var gateway = new FakePaymentGateway();
        var paymentIntent = new CreatePaymentIntentCommandHandler(context, _db.TenantContext, gateway,
            Microsoft.Extensions.Options.Options.Create(new InventoryX.Application.Options.PaystackOptions
            { CallbackUrl = "https://billing.example/settings/billing" }), new FakeProtector());

        var result = await new StartRecoveryCommandHandler(context, paymentIntent)
            .Handle(new StartRecoveryCommand("card", null, null, "recovery-1"), CancellationToken.None);

        result.Status.Should().Be("pending");
        (await context.PaymentIntents.SingleAsync()).Purpose.Should().Be(PaymentIntentPurpose.Recovery);
        (await context.BillingQuotes.SingleAsync()).PriceVersion.Should().StartWith("recovery:");
    }

    private async Task<(PlanDefinition Free, PlanDefinition Paid, Subscription Subscription)> SeedPurchaseAsync(
        InventoryX.Infrastructure.Data.AppDbContext context)
    {
        _db.TenantContext.Role = "Owner";
        var free = new PlanDefinition { Name = "Free", Tier = PlanTier.Free };
        var paid = new PlanDefinition { Name = "Standard", Tier = PlanTier.Standard,
            MonthlyPrice = 199m, AnnualPrice = 1990m };
        var subscription = new Subscription { TenantId = _tenantId, PlanDefinitionId = free.Id,
            Status = SubscriptionStatus.Active };
        context.Tenants.Add(new Tenant { Id = _tenantId, Name = "Purchase shop", BillingEmail = "owner@example.com" });
        context.PlanDefinitions.AddRange(free, paid);
        context.Subscriptions.Add(subscription);
        await context.SaveChangesAsync();
        return (free, paid, subscription);
    }

    private sealed class FakePaymentGateway : IPaymentGateway
    {
        public int InitializationCalls { get; private set; }
        public int ChargeCalls { get; private set; }
        public string ChargeStatus { get; init; } = "pay_offline";
        public bool ThrowOnInitialize { get; init; }
        public PaymentInitializationRequest? LastInitializationRequest { get; private set; }
        public Task<PaymentInitializationResult> InitializeAuthorizationAsync(PaymentInitializationRequest request,
            CancellationToken cancellationToken = default)
        {
            InitializationCalls++;
            LastInitializationRequest = request;
            if (ThrowOnInitialize) throw new TimeoutException("Provider outcome unknown.");
            return Task.FromResult(new PaymentInitializationResult(request.Reference!,
                "https://checkout.example/card", "access-code"));
        }
        public Task<PaymentChargeResult> ChargeAsync(PaymentChargeRequest request,
            CancellationToken cancellationToken = default)
        {
            ChargeCalls++;
            return Task.FromResult(new PaymentChargeResult(request.Reference!, ChargeStatus, "Approve the prompt."));
        }
        public Task<PaymentVerificationResult> VerifyAsync(string reference,
            CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    private sealed class FakeProtector : IBillingSecretProtector
    {
        public string Protect(Guid tenantId, string secret) => "v1:" + secret;
        public string Unprotect(Guid tenantId, string ciphertext) => ciphertext[3..];
    }

    [Fact]
    public async Task Downgrade_requires_acknowledgement_when_usage_exceeds_target_limit_and_cancellation_reactivates()
    {
        await using var context = _db.CreateContext();
        var source = new PlanDefinition { Name = "Professional", Tier = PlanTier.Professional };
        var target = new PlanDefinition { Name = "Free", Tier = PlanTier.Free, MaxProducts = 1 };
        context.AddRange(source, target);
        context.Subscriptions.Add(new Subscription
        {
            PlanDefinitionId = source.Id, Status = SubscriptionStatus.Active,
            CurrentPeriodStart = DateTime.UtcNow.AddDays(-5), CurrentPeriodEnd = DateTime.UtcNow.AddDays(25),
        });
        context.Products.AddRange(new Product { Name = "Soap" }, new Product { Name = "Milk" });
        await context.SaveChangesAsync();

        var downgrade = new DowngradeSubscriptionCommandHandler(context);
        var blocked = () => downgrade.Handle(new DowngradeSubscriptionCommand { PlanDefinitionId = target.Id }, CancellationToken.None);
        await blocked.Should().ThrowAsync<InventoryX.Application.Exceptions.ConflictException>();
        var scheduled = await downgrade.Handle(new DowngradeSubscriptionCommand { PlanDefinitionId = target.Id, AcknowledgeOverLimit = true }, CancellationToken.None);
        (await context.Subscriptions.SingleAsync()).PendingPlanDefinitionId.Should().Be(target.Id);

        var cancelled = await new CancelSubscriptionCommandHandler(context).Handle(new CancelSubscriptionCommand(), CancellationToken.None);
        cancelled.PurgeAt.Should().NotBeNull();
        var active = await new ReactivateSubscriptionCommandHandler(context).Handle(new ReactivateSubscriptionCommand(), CancellationToken.None);
        active.CancelledAt.Should().BeNull();
    }

    [Fact]
    public async Task Cancellation_replaces_pending_downgrade_and_reversal_does_not_restore_it()
    {
        await using var context = _db.CreateContext();
        var source = new PlanDefinition { Name = "Professional", Tier = PlanTier.Professional,
            MonthlyPrice = 399m };
        var target = new PlanDefinition { Name = "Standard", Tier = PlanTier.Standard,
            MonthlyPrice = 199m };
        context.PlanDefinitions.AddRange(source, target);
        context.Subscriptions.Add(new Subscription
        {
            TenantId = _tenantId, PlanDefinitionId = source.Id,
            Status = SubscriptionStatus.Active,
            CurrentPeriodStart = DateTime.UtcNow.AddDays(-5),
            CurrentPeriodEnd = DateTime.UtcNow.AddDays(25),
        });
        await context.SaveChangesAsync();

        await new DowngradeSubscriptionCommandHandler(context).Handle(
            new DowngradeSubscriptionCommand { PlanDefinitionId = target.Id }, CancellationToken.None);
        var scheduledDowngrade = await context.ScheduledSubscriptionChanges.SingleAsync();
        scheduledDowngrade.Kind.Should().Be(ScheduledChangeKind.Downgrade);
        scheduledDowngrade.Status.Should().Be(ScheduledChangeStatus.Scheduled);

        await new CancelSubscriptionCommandHandler(context).Handle(
            new CancelSubscriptionCommand(), CancellationToken.None);
        scheduledDowngrade.Status.Should().Be(ScheduledChangeStatus.Reversed);
        var scheduledCancel = await context.ScheduledSubscriptionChanges.SingleAsync(change =>
            change.Kind == ScheduledChangeKind.Cancel);
        scheduledCancel.Status.Should().Be(ScheduledChangeStatus.Scheduled);
        scheduledCancel.EffectiveAt.Should().Be((await context.Subscriptions.SingleAsync()).CurrentPeriodEnd);

        await new ReactivateSubscriptionCommandHandler(context).Handle(
            new ReactivateSubscriptionCommand(), CancellationToken.None);
        scheduledCancel.Status.Should().Be(ScheduledChangeStatus.Reversed);
        scheduledDowngrade.Status.Should().Be(ScheduledChangeStatus.Reversed);
        (await context.Subscriptions.SingleAsync()).PendingPlanDefinitionId.Should().BeNull();
    }

    public void Dispose() => _db.Dispose();
}
