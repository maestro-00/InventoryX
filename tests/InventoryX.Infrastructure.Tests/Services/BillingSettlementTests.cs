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
        context.AddRange(subscription, period, intent, attempt);
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
}
