using System.Diagnostics;
using FluentAssertions;
using InventoryX.Application.Queries.RequestHandlers.Billing;
using InventoryX.Application.Queries.Requests.Billing;
using InventoryX.Common.Tests;
using InventoryX.Domain.Models.Tenancy;

namespace InventoryX.Application.Tests.Billing;

public sealed class BillingReadPerformanceTests : IDisposable
{
    private readonly Guid tenantId = Guid.NewGuid();
    private readonly TestDb db;

    public BillingReadPerformanceTests() => db = new TestDb(tenantId, "owner");

    [Fact]
    public async Task Authenticated_subscription_reads_keep_p95_under_500ms_in_test_environment()
    {
        await using var context = db.CreateContext();
        var plan = new PlanDefinition { Name = "Standard", Tier = PlanTier.Standard, MonthlyPrice = 199m };
        context.Tenants.Add(new Tenant { Id = tenantId, Name = "Performance shop", Currency = "GHS" });
        context.PlanDefinitions.Add(plan);
        context.Subscriptions.Add(new Subscription
        {
            TenantId = tenantId, PlanDefinitionId = plan.Id, Status = SubscriptionStatus.Active,
            CurrentPeriodStart = DateTime.UtcNow.AddDays(-1), CurrentPeriodEnd = DateTime.UtcNow.AddDays(29),
        });
        await context.SaveChangesAsync();
        var handler = new GetCurrentBillingSubscriptionQueryHandler(context);
        var samples = new List<double>();
        for (var i = 0; i < 20; i++)
        {
            var stopwatch = Stopwatch.StartNew();
            await handler.Handle(new GetCurrentBillingSubscriptionQuery(), CancellationToken.None);
            stopwatch.Stop();
            samples.Add(stopwatch.Elapsed.TotalMilliseconds);
        }

        var p95 = samples.OrderBy(value => value).ElementAt((int)Math.Ceiling(samples.Count * .95) - 1);
        p95.Should().BeLessThan(500, $"billing subscription read p95 was {p95:F1}ms");
    }

    public void Dispose() => db.Dispose();
}
