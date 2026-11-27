using FluentAssertions;
using InventoryX.Application.Queries.RequestHandlers.Billing;
using InventoryX.Application.Queries.Requests.Billing;
using InventoryX.Application.Commands.RequestHandlers.Billing;
using InventoryX.Application.Commands.Requests.Billing;
using InventoryX.Common.Tests;
using InventoryX.Domain.Models;
using InventoryX.Domain.Models.Catalog;
using InventoryX.Domain.Models.Inventory;
using InventoryX.Domain.Models.Selling;
using InventoryX.Domain.Models.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace InventoryX.Application.Tests.Billing;

public sealed class BillingQueryTests : IDisposable
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly TestDb _db;

    public BillingQueryTests() => _db = new TestDb(_tenantId, "owner-1");

    [Fact]
    public async Task Plans_and_subscription_expose_limits_features_and_usage()
    {
        await using var context = _db.CreateContext();
        var plan = new PlanDefinition
        {
            Name = "Standard", Tier = PlanTier.Standard, MonthlyPrice = 199m,
            MaxProducts = 5000, MonthlySaleCap = 3000, PurchaseOrders = true,
        };
        context.PlanDefinitions.Add(plan);
        context.Subscriptions.Add(new Subscription
        {
            PlanDefinitionId = plan.Id, Status = SubscriptionStatus.Active,
            CurrentPeriodStart = DateTime.UtcNow, CurrentPeriodEnd = DateTime.UtcNow.AddMonths(1),
        });
        context.UsageCounters.Add(new UsageCounter
        {
            Metric = UsageMetric.SalesThisMonth, PeriodKey = DateTime.UtcNow.ToString("yyyy-MM"), Count = 12,
        });
        await context.SaveChangesAsync();

        var plans = await new GetBillingPlansQueryHandler(context).Handle(new GetBillingPlansQuery(), CancellationToken.None);
        plans.Should().ContainSingle(item => item.Id == plan.Id && item.Features["purchaseOrders"]);
        plans.Single().Currency.Should().Be("GHS");
        plans.Single().ContactSales.Should().BeFalse();
        plans.Single().AnnualSavings.Should().Be(12 * plan.MonthlyPrice - plan.AnnualPrice);
        var subscription = await new GetCurrentBillingSubscriptionQueryHandler(context)
            .Handle(new GetCurrentBillingSubscriptionQuery(), CancellationToken.None);
        subscription.Plan.Should().Be("Standard");
        subscription.Usage.Should().ContainSingle(item => item.Metric == "salesThisMonth" && item.Current == 12 && item.Limit == 3000);
    }

    [Fact]
    public async Task Subscription_counts_existing_resources_when_usage_counters_are_missing()
    {
        await using var context = _db.CreateContext();
        var plan = new PlanDefinition { Name = "Free", Tier = PlanTier.Free };
        context.PlanDefinitions.Add(plan);
        context.Subscriptions.Add(new Subscription
        {
            PlanDefinitionId = plan.Id, Status = SubscriptionStatus.Active,
            CurrentPeriodStart = DateTime.UtcNow, CurrentPeriodEnd = DateTime.UtcNow.AddMonths(1),
        });
        var location = new Location { Name = "Main Shop" };
        context.Locations.Add(location);
        context.Products.AddRange(new Product { Name = "Soap" }, new Product { Name = "Milk" });
        context.Registers.Add(new Register { Name = "Front till", LocationId = location.Id });
        context.Users.Add(new User { Id = "owner-1", UserName = "owner", TenantId = _tenantId, IsOwner = true });
        await context.SaveChangesAsync();

        var subscription = await new GetCurrentBillingSubscriptionQueryHandler(context)
            .Handle(new GetCurrentBillingSubscriptionQuery(), CancellationToken.None);

        subscription.Usage.Should().ContainSingle(item => item.Metric == "locations" && item.Current == 1);
        subscription.Usage.Should().ContainSingle(item => item.Metric == "users" && item.Current == 1);
        subscription.Usage.Should().ContainSingle(item => item.Metric == "products" && item.Current == 2);
        subscription.Usage.Should().ContainSingle(item => item.Metric == "registers" && item.Current == 1);
    }

    [Fact]
    public async Task Subscription_exposes_next_renewal_and_safe_active_method_summary()
    {
        await using var context = _db.CreateContext();
        var plan = new PlanDefinition { Name = "Standard", Tier = PlanTier.Standard,
            MonthlyPrice = 199m, AnnualPrice = 1990m };
        var method = new PaymentMethod { Kind = PaymentMethodKind.Card,
            RenewalMode = PaymentRenewalMode.Automatic, Provider = "paystack",
            Brand = "Visa", MaskedIdentifier = "**** 4242", ExpiryMonth = 12, ExpiryYear = 2028,
            EncryptedAuthorizationCode = "v1:opaque" };
        method.Activate(DateTime.UtcNow.AddDays(-1));
        var dueAt = DateTime.UtcNow.AddDays(20);
        context.PlanDefinitions.Add(plan);
        context.PaymentMethods.Add(method);
        context.Subscriptions.Add(new Subscription { PlanDefinitionId = plan.Id,
            BillingCycle = BillingCycle.Annual, Status = SubscriptionStatus.Active,
            CurrentPaymentMethodId = method.Id, CurrentPeriodEnd = dueAt });
        await context.SaveChangesAsync();

        var result = await new GetCurrentBillingSubscriptionQueryHandler(context)
            .Handle(new GetCurrentBillingSubscriptionQuery(), CancellationToken.None);

        result.NextRenewalAt.Should().Be(dueAt);
        result.NextRenewalAmount.Should().Be(1990m);
        result.MethodSummary!.Kind.Should().Be("card");
        result.MethodSummary.MaskedIdentifier.Should().Be("**** 4242");
        result.MethodSummary.Brand.Should().Be("Visa");
    }

    [Fact]
    public async Task Owner_can_update_billing_contact_and_tax_number()
    {
        await using var context = _db.CreateContext();
        var plan = new PlanDefinition { Name = "Standard", Tier = PlanTier.Standard };
        context.Tenants.Add(new Tenant { Id = _tenantId, Name = "Shop", BillingEmail = "old@example.com" });
        context.PlanDefinitions.Add(plan);
        context.Subscriptions.Add(new Subscription { PlanDefinitionId = plan.Id, Status = SubscriptionStatus.Active });
        await context.SaveChangesAsync();

        await new UpdateBillingContactCommandHandler(context).Handle(
            new UpdateBillingContactCommand(" billing@example.com ", " TAX-123 "), CancellationToken.None);

        var tenant = await context.Tenants.SingleAsync();
        tenant.BillingEmail.Should().Be("billing@example.com");
        tenant.BillingTaxNumber.Should().Be("TAX-123");
    }

    public void Dispose() => _db.Dispose();
}
