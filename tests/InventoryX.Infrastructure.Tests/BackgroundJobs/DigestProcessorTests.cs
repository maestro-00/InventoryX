using FluentAssertions;
using InventoryX.Common.Tests;
using InventoryX.Domain.Models;
using InventoryX.Domain.Models.Auditing;
using InventoryX.Infrastructure.BackgroundJobs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace InventoryX.Infrastructure.Tests.BackgroundJobs;

public sealed class DigestProcessorTests : IDisposable
{
    private readonly TestDb _db = new(Guid.NewGuid(), "user-1");

    [Fact]
    public async Task Daily_digest_summarizes_occurrences_and_is_sent_once_per_period()
    {
        var now = new DateTime(2026, 8, 7, 10, 0, 0, DateTimeKind.Utc);
        await using var context = _db.CreateContext();
        await SeedUserAndPreference(context, NotificationType.DailyDigest);
        context.Notifications.Add(new Notification
        {
            Type = NotificationType.LowStock, Channel = NotificationChannel.InApp,
            ConsolidationKey = "low:1", Title = "Low stock", OccurrenceCount = 3,
            LastRaisedAt = now.Date.AddHours(-2),
        });
        await context.SaveChangesAsync();
        var processor = new DigestProcessor(context, _db.TenantContext, NullLogger<DigestProcessor>.Instance);

        await processor.ProcessAsync(now, CancellationToken.None);
        await processor.ProcessAsync(now.AddHours(1), CancellationToken.None);

        var outbox = await context.OutboxMessages.SingleAsync();
        var payload = EmailOutbox.Deserialize(outbox);
        payload.To.Should().Be("user@example.com");
        payload.HtmlBody.Should().Contain("LowStock").And.Contain("3 occurrences");
        outbox.IdempotencyKey.Should().Contain("daily:");
        (await context.NotificationDigestDeliveries.SingleAsync()).OccurrenceCount.Should().Be(3);
    }

    [Fact]
    public async Task Weekly_digest_uses_the_most_recent_completed_monday_to_sunday_window()
    {
        var now = new DateTime(2026, 8, 7, 10, 0, 0, DateTimeKind.Utc);
        await using var context = _db.CreateContext();
        await SeedUserAndPreference(context, NotificationType.WeeklyDigest);
        context.Notifications.Add(new Notification
        {
            Type = NotificationType.PoOverdue, Channel = NotificationChannel.InApp,
            ConsolidationKey = "po:1", Title = "PO overdue", LastRaisedAt = new DateTime(2026, 7, 30, 9, 0, 0, DateTimeKind.Utc),
        });
        await context.SaveChangesAsync();
        var processor = new DigestProcessor(context, _db.TenantContext, NullLogger<DigestProcessor>.Instance);

        await processor.ProcessAsync(now, CancellationToken.None);

        var payload = EmailOutbox.Deserialize(await context.OutboxMessages.SingleAsync());
        payload.To.Should().Be("user@example.com");
        payload.Subject.Should().Contain("weekly");
        var delivery = await context.NotificationDigestDeliveries.SingleAsync();
        delivery.PeriodStart.Should().Be(new DateTime(2026, 7, 27, 0, 0, 0, DateTimeKind.Utc));
        delivery.PeriodEnd.Should().Be(new DateTime(2026, 8, 3, 0, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public async Task Disabled_digest_preference_does_not_send()
    {
        await using var context = _db.CreateContext();
        await SeedUserAndPreference(context, NotificationType.DailyDigest, enabled: false);
        var processor = new DigestProcessor(context, _db.TenantContext, NullLogger<DigestProcessor>.Instance);

        await processor.ProcessAsync(DateTime.UtcNow, CancellationToken.None);

        (await context.OutboxMessages.CountAsync()).Should().Be(0);
        (await context.NotificationDigestDeliveries.CountAsync()).Should().Be(0);
    }

    private static async Task SeedUserAndPreference(
        InventoryX.Infrastructure.Data.AppDbContext context, NotificationType type, bool enabled = true)
    {
        context.Users.Add(new User
        {
            Id = "user-1", UserName = "user@example.com", NormalizedUserName = "USER@EXAMPLE.COM",
            Email = "user@example.com", NormalizedEmail = "USER@EXAMPLE.COM", TenantId = context.CurrentTenantId,
        });
        context.NotificationPreferences.Add(new NotificationPreference
        {
            UserId = "user-1", Type = type, Channel = NotificationChannel.Email, IsEnabled = enabled,
        });
        await context.SaveChangesAsync();
    }

    public void Dispose() => _db.Dispose();
}
