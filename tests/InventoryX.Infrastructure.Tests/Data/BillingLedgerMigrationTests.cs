using FluentAssertions;
using InventoryX.Domain.Models.Tenancy;
using InventoryX.Infrastructure.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace InventoryX.Infrastructure.Tests.Data;

public sealed class BillingLedgerMigrationTests
{
    [Fact]
    public void PostgreSql_migration_backfills_periods_without_charge_or_legacy_method_activation()
    {
        using var context = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Database=billing_script_only;Username=unused;Password=unused").Options);
        var script = context.GetService<IMigrator>().GenerateScript();
        script.Should().Contain("CREATE TABLE \"PaystackEventInbox\"");
        script.Should().Contain("legacy-migration-no-charge");
        script.Should().Contain("INSERT INTO \"BillingPeriods\"");
        script.Should().Contain("UPDATE \"Subscriptions\"");
        script.Should().NotContain("INSERT INTO \"PaymentMethods\"");
        script.Should().NotContain("DELETE FROM \"BillingInvoices\"");
        script.Should().NotContain("UPDATE \"BillingInvoices\"");
        script.Should().Contain("CREATE UNIQUE INDEX \"IX_Subscriptions_TenantId\"");
    }

    [Fact]
    public void PostgreSql_model_requires_one_subscription_and_period_and_unique_provider_reference()
    {
        using var context = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Database=billing_model_only;Username=unused;Password=unused").Options);
        var subscription = context.Model.FindEntityType(typeof(Subscription))!;
        subscription.GetIndexes().Should().Contain(index => index.IsUnique &&
            index.Properties.Select(property => property.Name).SequenceEqual(new[] { "TenantId" }));
        var period = context.Model.FindEntityType(typeof(BillingPeriod));
        period.Should().NotBeNull();
        period!.GetIndexes().Should().Contain(index => index.IsUnique &&
            index.Properties.Select(property => property.Name).SequenceEqual(new[] { "SubscriptionId", "StartAt", "Cycle" }));
        period.GetForeignKeys().Should().Contain(key => key.PrincipalEntityType.ClrType == typeof(Subscription) &&
            key.Properties.Select(property => property.Name).SequenceEqual(new[] { "TenantId", "SubscriptionId" }));
        var attempt = context.Model.FindEntityType(typeof(PaymentAttempt));
        attempt.Should().NotBeNull();
        attempt!.GetIndexes().Should().Contain(index => index.IsUnique &&
            index.Properties.Select(property => property.Name).SequenceEqual(new[] { "Reference" }));
    }

    [Fact]
    public async Task SQLite_rejects_duplicate_period_and_provider_reference()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:;Foreign Keys=False");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
        await using var context = new AppDbContext(options);
        await context.Database.EnsureCreatedAsync();
        var tenantId = Guid.NewGuid();
        var subscriptionId = Guid.NewGuid();
        var start = new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc);
        BillingPeriod NewPeriod() => new()
        {
            TenantId = tenantId, SubscriptionId = subscriptionId, StartAt = start,
            EndAt = start.AddMonths(1), Cycle = BillingCycle.Monthly,
            PlanDefinitionId = Guid.NewGuid(),
        };
        context.Set<BillingPeriod>().Add(NewPeriod());
        await context.SaveChangesAsync();
        context.Set<BillingPeriod>().Add(NewPeriod());
        await FluentActions.Invoking(() => context.SaveChangesAsync()).Should().ThrowAsync<DbUpdateException>();

        context.ChangeTracker.Clear();
        context.Set<PaymentAttempt>().Add(new PaymentAttempt { TenantId = tenantId, Reference = "paystack-reference", PaymentIntentId = Guid.NewGuid() });
        await context.SaveChangesAsync();
        context.Set<PaymentAttempt>().Add(new PaymentAttempt { TenantId = tenantId, Reference = "paystack-reference", PaymentIntentId = Guid.NewGuid() });
        await FluentActions.Invoking(() => context.SaveChangesAsync()).Should().ThrowAsync<DbUpdateException>();
    }
}
