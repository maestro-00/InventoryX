using FluentAssertions;
using InventoryX.Common.Tests;
using InventoryX.Domain.Models.Auditing;
using InventoryX.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace InventoryX.Infrastructure.Tests.BackgroundJobs;

public sealed class BillingNotificationTests : IDisposable
{
    private readonly Guid tenantId = Guid.NewGuid();
    private readonly TestDb db;

    public BillingNotificationTests() => db = new TestDb(tenantId, "owner");

    [Fact]
    public async Task Repeated_failure_notice_is_consolidated_without_provider_secrets()
    {
        await using var context = db.CreateContext();
        var service = new NotificationService(context);

        await service.RaiseAsync(NotificationType.BillingFailure, "billing:failure:period-1",
            "Payment recovery required", "Recover billing before the deadline.");
        await service.RaiseAsync(NotificationType.BillingFailure, "billing:failure:period-1",
            "Payment recovery required", "Recover billing before the deadline.");

        var notification = await context.Notifications.SingleAsync();
        notification.OccurrenceCount.Should().Be(2);
        notification.Message.Should().NotContain("authorization").And.NotContain("secret");
    }

    public void Dispose() => db.Dispose();
}
