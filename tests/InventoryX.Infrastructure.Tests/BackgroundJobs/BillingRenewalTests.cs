using FluentAssertions;
using InventoryX.Application.Services.IServices;
using InventoryX.Common.Tests;
using InventoryX.Domain.Models.Tenancy;
using InventoryX.Infrastructure.BackgroundJobs;
using InventoryX.Infrastructure.Data;
using InventoryX.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace InventoryX.Infrastructure.Tests.BackgroundJobs;

public sealed class BillingRenewalTests : IDisposable
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly TestDb _db;

    public BillingRenewalTests() => _db = new TestDb();

    [Fact]
    public async Task Two_ticks_charge_and_invoice_one_anchored_card_period()
    {
        _db.TenantContext.TenantId = _tenantId;
        await using var context = _db.CreateContext();
        await SeedDueAsync(context, PaymentMethodKind.Card);
        var gateway = new FakeGateway { ChargeStatus = "success" };
        using var services = Services(context, gateway);
        var worker = new BillingWorker(services.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<BillingWorker>.Instance);

        await worker.ProcessAsync(CancellationToken.None);
        await worker.ProcessAsync(CancellationToken.None);

        gateway.ChargeCalls.Should().Be(1);
        (await context.PaymentAttempts.CountAsync()).Should().Be(1);
        (await context.PaymentIntents.CountAsync(item => item.Purpose == PaymentIntentPurpose.Renewal)).Should().Be(1);
        (await context.BillingInvoices.CountAsync()).Should().Be(1);
        (await context.Subscriptions.SingleAsync()).CurrentPeriodEnd.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public async Task Unknown_card_outcome_keeps_one_attempt_until_verification()
    {
        _db.TenantContext.TenantId = _tenantId;
        await using var context = _db.CreateContext();
        await SeedDueAsync(context, PaymentMethodKind.Card);
        var gateway = new FakeGateway { ThrowOnCharge = true, VerificationStatus = "pending" };
        using var services = Services(context, gateway);
        var worker = new BillingWorker(services.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<BillingWorker>.Instance);

        await worker.ProcessAsync(CancellationToken.None);
        await worker.ProcessAsync(CancellationToken.None);

        gateway.ChargeCalls.Should().Be(1);
        (await context.PaymentAttempts.SingleAsync()).Status.Should().Be(PaymentAttemptStatus.Unknown);
        (await context.BillingInvoices.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Mobile_money_renewal_requests_new_approval_and_never_reuses_authorization()
    {
        _db.TenantContext.TenantId = _tenantId;
        await using var context = _db.CreateContext();
        await SeedDueAsync(context, PaymentMethodKind.MobileMoney);
        var gateway = new FakeGateway { ChargeStatus = "pay_offline" };
        using var services = Services(context, gateway);
        var worker = new BillingWorker(services.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<BillingWorker>.Instance);

        await worker.ProcessAsync(CancellationToken.None);

        gateway.ChargeCalls.Should().Be(1);
        gateway.LastCharge!.AuthorizationCode.Should().BeNull();
        gateway.LastCharge.Msisdn.Should().Be("0241234567");
        (await context.PaymentAttempts.SingleAsync()).Status.Should().Be(PaymentAttemptStatus.AwaitingApproval);
        (await context.BillingInvoices.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Mobile_money_approval_is_prompted_before_due_date_without_early_grace()
    {
        _db.TenantContext.TenantId = _tenantId;
        await using var context = _db.CreateContext();
        await SeedDueAsync(context, PaymentMethodKind.MobileMoney, DateTime.UtcNow.AddHours(12));
        var gateway = new FakeGateway { ChargeStatus = "pay_offline", VerificationStatus = "pending" };
        using var services = Services(context, gateway);
        var worker = new BillingWorker(services.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<BillingWorker>.Instance);

        await worker.ProcessAsync(CancellationToken.None);
        await worker.ProcessAsync(CancellationToken.None);

        gateway.ChargeCalls.Should().Be(1);
        (await context.PaymentAttempts.SingleAsync()).Status.Should().Be(PaymentAttemptStatus.AwaitingApproval);
        (await context.Subscriptions.SingleAsync()).Status.Should().Be(SubscriptionStatus.Active);
        (await context.BillingInvoices.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Concurrent_ticks_do_not_charge_the_same_period_twice()
    {
        _db.TenantContext.TenantId = _tenantId;
        await using (var seed = _db.CreateContext())
            await SeedDueAsync(seed, PaymentMethodKind.Card);
        var gateway = new FakeGateway
        {
            ChargeStatus = "pending",
            VerificationStatus = "pending",
            ChargeEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously),
            ReleaseCharge = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously),
        };
        using var services = ScopedServices(gateway);
        var worker = new BillingWorker(services.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<BillingWorker>.Instance);

        var firstTick = worker.ProcessAsync(CancellationToken.None);
        await gateway.ChargeEntered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await worker.ProcessAsync(CancellationToken.None);
        gateway.ReleaseCharge.SetResult();
        await firstTick;

        await using var check = _db.CreateContext();
        gateway.ChargeCalls.Should().Be(1);
        (await check.PaymentAttempts.CountAsync()).Should().Be(1);
        (await check.PaymentIntents.CountAsync(item => item.Purpose == PaymentIntentPurpose.Renewal))
            .Should().Be(1);
    }

    [Fact]
    public async Task Scheduled_cancellation_at_paid_boundary_stops_the_next_charge()
    {
        _db.TenantContext.TenantId = _tenantId;
        await using var context = _db.CreateContext();
        await SeedDueAsync(context, PaymentMethodKind.Card);
        var subscription = await context.Subscriptions.SingleAsync();
        context.ScheduledSubscriptionChanges.Add(new ScheduledSubscriptionChange
        {
            TenantId = _tenantId, SubscriptionId = subscription.Id,
            Kind = ScheduledChangeKind.Cancel, EffectiveAt = subscription.CurrentPeriodEnd,
        });
        subscription.CancelledAt = subscription.CurrentPeriodEnd;
        await context.SaveChangesAsync();
        var gateway = new FakeGateway { ChargeStatus = "success" };
        using var services = Services(context, gateway);
        var worker = new BillingWorker(services.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<BillingWorker>.Instance);

        await worker.ProcessAsync(CancellationToken.None);

        gateway.ChargeCalls.Should().Be(0);
        (await context.ScheduledSubscriptionChanges.SingleAsync()).Status.Should().Be(ScheduledChangeStatus.Applied);
        (await context.Subscriptions.SingleAsync()).Status.Should().Be(SubscriptionStatus.PurgePending);
    }

    [Fact]
    public async Task Past_due_subscription_is_not_retried_twice_in_one_day()
    {
        _db.TenantContext.TenantId = _tenantId;
        await using var context = _db.CreateContext();
        await SeedDueAsync(context, PaymentMethodKind.Card);
        var subscription = await context.Subscriptions.SingleAsync();
        subscription.Status = SubscriptionStatus.PastDue;
        subscription.GraceExpiresAt = DateTime.UtcNow.AddDays(6);
        subscription.LastChargeAttemptAt = DateTime.UtcNow.AddHours(-12);
        await context.SaveChangesAsync();
        var gateway = new FakeGateway { ChargeStatus = "success" };
        using var services = Services(context, gateway);
        var worker = new BillingWorker(services.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<BillingWorker>.Instance);

        await worker.ProcessAsync(CancellationToken.None);

        gateway.ChargeCalls.Should().Be(0);
    }

    private async Task SeedDueAsync(AppDbContext context, PaymentMethodKind kind, DateTime? dueAt = null)
    {
        var due = dueAt ?? DateTime.UtcNow.AddMinutes(-2);
        var plan = new PlanDefinition { Name = "Standard", Tier = PlanTier.Standard, MonthlyPrice = 30m };
        var method = new PaymentMethod { TenantId = _tenantId, Kind = kind,
            RenewalMode = kind == PaymentMethodKind.Card ? PaymentRenewalMode.Automatic : PaymentRenewalMode.OwnerApproval,
            Provider = kind == PaymentMethodKind.Card ? "paystack" : "mtn", MaskedIdentifier = "**** 4567",
            EncryptedAuthorizationCode = kind == PaymentMethodKind.Card ? "v1:AUTH-1" : null,
            EncryptedPhoneNumber = kind == PaymentMethodKind.MobileMoney ? "v1:0241234567" : null };
        method.Activate(due.AddDays(-20));
        context.Tenants.Add(new Tenant { Id = _tenantId, Name = "Shop", BillingEmail = "owner@example.com" });
        context.PlanDefinitions.Add(plan);
        context.PaymentMethods.Add(method);
        context.Subscriptions.Add(new Subscription { TenantId = _tenantId, PlanDefinitionId = plan.Id,
            Status = SubscriptionStatus.Active, BillingCycle = BillingCycle.Monthly,
            BillingAnchor = due.AddMonths(-1), CurrentPeriodStart = due.AddMonths(-1),
            CurrentPeriodEnd = due, CurrentPaymentMethodId = method.Id });
        await context.SaveChangesAsync();
    }

    private ServiceProvider Services(AppDbContext context, IPaymentGateway gateway)
    {
        var services = new ServiceCollection();
        services.AddSingleton(context);
        services.AddSingleton(gateway);
        services.AddSingleton<IPaymentGateway>(gateway);
        services.AddSingleton<ITenantContext>(_db.TenantContext);
        services.AddSingleton<IBillingSecretProtector>(new FakeProtector());
        services.AddScoped<IBillingSettlementService, BillingSettlementService>();
        return services.BuildServiceProvider();
    }

    private ServiceProvider ScopedServices(IPaymentGateway gateway)
    {
        var services = new ServiceCollection();
        services.AddScoped(_ => _db.CreateContext());
        services.AddSingleton<IPaymentGateway>(gateway);
        services.AddSingleton<ITenantContext>(_db.TenantContext);
        services.AddSingleton<IBillingSecretProtector>(new FakeProtector());
        services.AddScoped<IBillingSettlementService, BillingSettlementService>();
        return services.BuildServiceProvider();
    }

    private sealed class FakeProtector : IBillingSecretProtector
    {
        public string Protect(Guid tenantId, string secret) => "v1:" + secret;
        public string Unprotect(Guid tenantId, string ciphertext) => ciphertext[3..];
    }

    private sealed class FakeGateway : IPaymentGateway
    {
        public int ChargeCalls { get; private set; }
        public string ChargeStatus { get; init; } = "pending";
        public string VerificationStatus { get; init; } = "success";
        public bool ThrowOnCharge { get; init; }
        public PaymentChargeRequest? LastCharge { get; private set; }
        public TaskCompletionSource? ChargeEntered { get; init; }
        public TaskCompletionSource? ReleaseCharge { get; init; }
        public Task<PaymentInitializationResult> InitializeAuthorizationAsync(PaymentInitializationRequest request,
            CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public async Task<PaymentChargeResult> ChargeAsync(PaymentChargeRequest request,
            CancellationToken cancellationToken = default)
        {
            ChargeCalls++;
            LastCharge = request;
            ChargeEntered?.TrySetResult();
            if (ReleaseCharge is not null)
                await ReleaseCharge.Task.WaitAsync(cancellationToken);
            if (ThrowOnCharge) throw new TimeoutException("Unknown provider outcome.");
            return new PaymentChargeResult(request.Reference!, ChargeStatus);
        }
        public Task<PaymentVerificationResult> VerifyAsync(string reference,
            CancellationToken cancellationToken = default) => Task.FromResult(
                new PaymentVerificationResult(reference, VerificationStatus, 30m, "GHS", null, DateTime.UtcNow));
    }

    public void Dispose() => _db.Dispose();
}
