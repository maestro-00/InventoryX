using AutoMapper;
using FluentAssertions;
using InventoryX.Application.Commands.RequestHandlers.Notifications;
using InventoryX.Application.Commands.Requests.Notifications;
using InventoryX.Application.Queries.RequestHandlers.Notifications;
using InventoryX.Application.Queries.Requests.Notifications;
using InventoryX.Application.Validators.Notifications;
using InventoryX.Application.Extensions;
using InventoryX.Application.Exceptions;
using InventoryX.Common.Tests;
using InventoryX.Domain.Models.Auditing;
using InventoryX.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace InventoryX.Application.Tests.Notifications;

public sealed class NotificationHandlersTests : IDisposable
{
    private readonly TestDb _db = new(Guid.NewGuid(), "user-1");
    private readonly IMapper _mapper = new MapperConfiguration(
        configuration => configuration.AddProfile<NotificationMappingProfile>(), NullLoggerFactory.Instance).CreateMapper();

    [Fact]
    public async Task Feed_is_paged_user_scoped_and_exposes_consolidated_occurrences()
    {
        await using var context = _db.CreateContext();
        var service = new NotificationService(context);
        await service.RaiseAsync(NotificationType.LowStock, "low:1", "Low stock");
        await service.RaiseAsync(NotificationType.LowStock, "low:1", "Still low");
        await service.RaiseAsync(NotificationType.BillingFailure, "billing:1", "Payment failed", userId: "user-1");
        await service.RaiseAsync(NotificationType.BillingFailure, "billing:2", "Other user", userId: "user-2");

        var result = await new GetNotificationsQueryHandler(context, _db.TenantContext, _mapper)
            .Handle(new GetNotificationsQuery { Page = 1, PageSize = 1 }, CancellationToken.None);

        result.TotalCount.Should().Be(2);
        result.Items.Should().ContainSingle();
        (await context.Notifications.SingleAsync(item => item.ConsolidationKey == "low:1"))
            .OccurrenceCount.Should().Be(2);
    }

    [Fact]
    public async Task Read_and_read_all_only_change_notifications_visible_to_current_user()
    {
        await using var context = _db.CreateContext();
        context.Notifications.AddRange(
            NewNotification("global"),
            NewNotification("mine", "user-1"),
            NewNotification("other", "user-2"));
        await context.SaveChangesAsync();
        var mine = await context.Notifications.SingleAsync(item => item.ConsolidationKey == "mine");

        await new MarkNotificationReadCommandHandler(context, _db.TenantContext)
            .Handle(new MarkNotificationReadCommand(mine.Id), CancellationToken.None);
        var changed = await new MarkAllNotificationsReadCommandHandler(context, _db.TenantContext)
            .Handle(new MarkAllNotificationsReadCommand(), CancellationToken.None);

        changed.Should().Be(1);
        var readIds = await context.NotificationReadStates.Select(item => item.NotificationId).ToListAsync();
        readIds.Should().Contain(await context.Notifications.Where(item => item.ConsolidationKey == "global").Select(item => item.Id).SingleAsync());
        readIds.Should().Contain(mine.Id);
        readIds.Should().NotContain(await context.Notifications.Where(item => item.ConsolidationKey == "other").Select(item => item.Id).SingleAsync());
    }

    [Fact]
    public async Task Read_rejects_a_notification_targeted_to_another_user()
    {
        await using var context = _db.CreateContext();
        var other = NewNotification("other", "user-2");
        context.Notifications.Add(other);
        await context.SaveChangesAsync();

        var action = () => new MarkNotificationReadCommandHandler(context, _db.TenantContext)
            .Handle(new MarkNotificationReadCommand(other.Id), CancellationToken.None);

        await action.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Preference_matrix_returns_defaults_and_persists_user_overrides()
    {
        await using var context = _db.CreateContext();
        var get = new GetNotificationPreferencesQueryHandler(context, _db.TenantContext, _mapper);
        var defaults = await get.Handle(new GetNotificationPreferencesQuery(), CancellationToken.None);
        defaults.Should().Contain(item => item.Type == NotificationType.LowStock && item.Channel == NotificationChannel.InApp && item.IsEnabled);
        defaults.Should().Contain(item => item.Type == NotificationType.LowStock && item.Channel == NotificationChannel.Email && !item.IsEnabled);

        var result = await new UpdateNotificationPreferencesCommandHandler(context, _db.TenantContext, _mapper).Handle(
            new UpdateNotificationPreferencesCommand
            {
                Preferences =
                [
                    new NotificationPreferenceInput(NotificationType.LowStock, NotificationChannel.Email, true, 5m),
                    new NotificationPreferenceInput(NotificationType.DailyDigest, NotificationChannel.Email, true, null),
                ],
            }, CancellationToken.None);

        result.Should().Contain(item => item.Type == NotificationType.LowStock && item.Channel == NotificationChannel.Email && item.IsEnabled && item.Threshold == 5m);
        (await context.NotificationPreferences.CountAsync()).Should().Be(2);
    }

    [Fact]
    public void Preference_validator_rejects_negative_thresholds_and_duplicate_pairs()
    {
        var command = new UpdateNotificationPreferencesCommand
        {
            Preferences =
            [
                new NotificationPreferenceInput(NotificationType.LowStock, NotificationChannel.Email, true, -1m),
                new NotificationPreferenceInput(NotificationType.LowStock, NotificationChannel.Email, false, null),
            ],
        };

        new UpdateNotificationPreferencesCommandValidator().Validate(command).IsValid.Should().BeFalse();
    }

    private static Notification NewNotification(string key, string? userId = null) => new()
    {
        Type = NotificationType.LowStock, Channel = NotificationChannel.InApp, ConsolidationKey = key,
        Title = key, UserId = userId, LastRaisedAt = DateTime.UtcNow,
    };

    public void Dispose() => _db.Dispose();
}
