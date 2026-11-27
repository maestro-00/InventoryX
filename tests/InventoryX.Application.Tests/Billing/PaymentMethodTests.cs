using FluentAssertions;
using InventoryX.Application.Commands.RequestHandlers.Billing;
using InventoryX.Application.Commands.Requests.Billing;
using InventoryX.Application.Services.IServices;
using InventoryX.Common.Tests;
using InventoryX.Domain.Models.Tenancy;
using Moq;
using Microsoft.EntityFrameworkCore;

namespace InventoryX.Application.Tests.Billing;

public sealed class PaymentMethodTests : IDisposable
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly TestDb _db;

    public PaymentMethodTests()
    {
        _db = new TestDb(_tenantId, "owner-1");
        _db.TenantContext.Role = "Owner";
    }

    [Fact]
    public async Task Mobile_money_replacement_preserves_active_method_until_verified()
    {
        await using var context = _db.CreateContext();
        var tenant = new Tenant { Id = _tenantId, Name = "Shop", BillingEmail = "owner@example.com" };
        var plan = new PlanDefinition { Name = "Standard", Tier = PlanTier.Standard, MonthlyPrice = 199m };
        context.AddRange(tenant, plan);
        var activeMethod = new PaymentMethod
        {
            Kind = PaymentMethodKind.MobileMoney, RenewalMode = PaymentRenewalMode.OwnerApproval,
            Provider = "telecel", MaskedIdentifier = "***1234", EncryptedPhoneNumber = "v1:old",
        };
        activeMethod.Activate(DateTime.UtcNow.AddDays(-1));
        context.PaymentMethods.Add(activeMethod);
        context.Subscriptions.Add(new Subscription
        {
            PlanDefinitionId = plan.Id, Status = SubscriptionStatus.Active,
            CurrentPeriodStart = DateTime.UtcNow, CurrentPeriodEnd = DateTime.UtcNow.AddMonths(1),
            CurrentPaymentMethodId = activeMethod.Id, PaymentMethodKind = "mobile_money",
            PaymentProvider = "telecel", PaymentMethodRef = "old-reference",
        });
        await context.SaveChangesAsync();
        var gateway = new Mock<IPaymentGateway>();
        gateway.Setup(item => item.ChargeAsync(It.Is<PaymentChargeRequest>(request =>
                request.MobileMoneyProvider == "mtn" && request.Email == tenant.BillingEmail &&
                request.Amount == 1m), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PaymentChargeRequest request, CancellationToken _) =>
                new PaymentChargeResult(request.Reference!, "pay_offline", "Approve on phone"));

        var command = new InitializePaymentMethodCommand { Channel = "mobile_money", Provider = "mtn",
            Msisdn = "0241234567", IdempotencyKey = "replace-momo-1" };
        var handler = NewHandler(context, gateway.Object);
        var result = await handler.Handle(command, CancellationToken.None);
        (await handler.Handle(command, CancellationToken.None)).IntentId.Should().Be(result.IntentId);
        await FluentActions.Invoking(() => handler.Handle(new InitializePaymentMethodCommand
        {
            Channel = "mobile_money", Provider = "telecel", Msisdn = "0241234567",
            IdempotencyKey = command.IdempotencyKey,
        }, CancellationToken.None)).Should().ThrowAsync<InventoryX.Application.Exceptions.ConflictException>();
        gateway.Verify(item => item.ChargeAsync(It.IsAny<PaymentChargeRequest>(),
            It.IsAny<CancellationToken>()), Times.Once);

        result.Status.Should().Be("awaiting_approval");
        result.Amount.Should().Be(1m);
        var subscription = await context.Subscriptions.SingleAsync();
        subscription.CurrentPaymentMethodId.Should().Be(activeMethod.Id);
        subscription.PaymentProvider.Should().Be("telecel");
        subscription.PaymentMethodRef.Should().Be("old-reference");
        (await context.PaymentMethods.SingleAsync(item => item.Id == activeMethod.Id))
            .Status.Should().Be(PaymentMethodStatus.Active);
        var pending = await context.PaymentMethods.SingleAsync(item => item.Id == result.PendingMethodId);
        pending.Status.Should().Be(PaymentMethodStatus.Pending);
        pending.EncryptedPhoneNumber.Should().StartWith("v1:");
        pending.EncryptedAuthorizationCode.Should().BeNull();
        (await context.PaymentAttempts.SingleAsync()).PaymentMethodId.Should().Be(pending.Id);
    }

    [Fact]
    public async Task Failed_replacement_initialization_preserves_current_method()
    {
        await using var context = _db.CreateContext();
        var tenant = new Tenant { Id = _tenantId, Name = "Shop", BillingEmail = "owner@example.com" };
        var plan = new PlanDefinition { Name = "Standard", Tier = PlanTier.Standard, MonthlyPrice = 199m };
        context.AddRange(tenant, plan);
        context.Subscriptions.Add(new Subscription
        {
            PlanDefinitionId = plan.Id, Status = SubscriptionStatus.Active,
            PaymentMethodKind = "card", PaymentMethodRef = "old-card",
            CurrentPeriodEnd = DateTime.UtcNow.AddMonths(1),
        });
        await context.SaveChangesAsync();
        var gateway = new Mock<IPaymentGateway>();
        gateway.Setup(item => item.InitializeAuthorizationAsync(It.IsAny<PaymentInitializationRequest>(),
                It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("provider unavailable"));

        var act = () => NewHandler(context, gateway.Object).Handle(
            new InitializePaymentMethodCommand { Channel = "card", IdempotencyKey = "replace-card-failed" },
            CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>();
        var subscription = await context.Subscriptions.SingleAsync();
        subscription.PaymentMethodKind.Should().Be("card");
        subscription.PaymentMethodRef.Should().Be("old-card");
        (await context.PaymentAttempts.SingleAsync()).Status.Should().Be(PaymentAttemptStatus.Unknown);
    }

    private InitializePaymentMethodCommandHandler NewHandler(InventoryX.Infrastructure.Data.AppDbContext context,
        IPaymentGateway gateway) => new(context, gateway, _db.TenantContext, new FakeProtector(),
        Microsoft.Extensions.Options.Options.Create(new InventoryX.Application.Options.PaystackOptions
        { CallbackUrl = "https://billing.example/settings/billing" }));

    private sealed class FakeProtector : IBillingSecretProtector
    {
        public string Protect(Guid tenantId, string secret) => "v1:" + secret;
        public string Unprotect(Guid tenantId, string ciphertext) => ciphertext[3..];
    }

    public void Dispose() => _db.Dispose();
}
