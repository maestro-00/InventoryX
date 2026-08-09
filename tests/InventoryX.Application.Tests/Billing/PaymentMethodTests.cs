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

    public PaymentMethodTests() => _db = new TestDb(_tenantId, "owner-1");

    [Fact]
    public async Task Mobile_money_initialization_records_method_metadata_and_returns_checkout_details()
    {
        await using var context = _db.CreateContext();
        var tenant = new Tenant { Id = _tenantId, Name = "Shop", BillingEmail = "owner@example.com" };
        var plan = new PlanDefinition { Name = "Standard", Tier = PlanTier.Standard, MonthlyPrice = 199m };
        context.AddRange(tenant, plan);
        context.Subscriptions.Add(new Subscription
        {
            PlanDefinitionId = plan.Id, Status = SubscriptionStatus.Active,
            CurrentPeriodStart = DateTime.UtcNow, CurrentPeriodEnd = DateTime.UtcNow.AddMonths(1),
        });
        await context.SaveChangesAsync();
        var gateway = new Mock<IPaymentGateway>();
        gateway.Setup(item => item.InitializeAuthorizationAsync(It.Is<PaymentInitializationRequest>(request =>
                request.Channel == "mobile_money" && request.Email == tenant.BillingEmail), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaymentInitializationResult("ref-1", "https://checkout", "access"));

        var result = await new InitializePaymentMethodCommandHandler(context, gateway.Object).Handle(
            new InitializePaymentMethodCommand { Channel = "mobile_money", Provider = "mtn", Msisdn = "0241234567" },
            CancellationToken.None);

        result.Reference.Should().Be("ref-1");
        var subscription = await context.Subscriptions.SingleAsync();
        subscription.PaymentMethodKind.Should().Be("mobile_money");
        subscription.PaymentProvider.Should().Be("mtn");
    }

    public void Dispose() => _db.Dispose();
}
