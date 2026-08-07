using System.Net;
using InventoryX.Application.Services.IServices;
using InventoryX.Domain.Models.Auditing;
using InventoryX.Infrastructure.Data;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace InventoryX.Infrastructure.BackgroundJobs;

public sealed class DigestWorker(IServiceScopeFactory scopes, ILogger<DigestWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopes.CreateScope();
                await scope.ServiceProvider.GetRequiredService<DigestProcessor>()
                    .ProcessAsync(DateTime.UtcNow, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception error)
            {
                logger.LogError(error, "Notification digest scan failed");
            }
            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }
}

public sealed class DigestProcessor(
    AppDbContext context,
    ITenantContext tenantContext,
    IEmailSender emailSender,
    ILogger<DigestProcessor> logger)
{
    public async Task ProcessAsync(DateTime utcNow, CancellationToken cancellationToken)
    {
        var candidates = await context.NotificationPreferences.IgnoreQueryFilters().AsNoTracking()
            .Where(item => item.IsEnabled && item.Channel == NotificationChannel.Email &&
                           (item.Type == NotificationType.DailyDigest || item.Type == NotificationType.WeeklyDigest))
            .Select(item => new { item.TenantId, item.UserId, item.Type })
            .ToListAsync(cancellationToken);

        foreach (var candidate in candidates)
        {
            try
            {
                await ProcessCandidateAsync(candidate.TenantId, candidate.UserId, candidate.Type, utcNow, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception error)
            {
                logger.LogError(error, "Notification digest failed for tenant {TenantId}, user {UserId}, type {DigestType}",
                    candidate.TenantId, candidate.UserId, candidate.Type);
            }
        }
    }

    private async Task ProcessCandidateAsync(
        Guid tenantId, string userId, NotificationType digestType, DateTime utcNow, CancellationToken cancellationToken)
    {
        var (start, end, periodKey, label) = Period(digestType, utcNow);
        if (await context.NotificationDigestDeliveries.IgnoreQueryFilters().AsNoTracking().AnyAsync(
                item => item.TenantId == tenantId && item.UserId == userId && item.DigestType == digestType &&
                        item.PeriodKey == periodKey, cancellationToken)) return;

        var email = await context.Users.AsNoTracking()
            .Where(user => user.Id == userId && user.TenantId == tenantId)
            .Select(user => user.Email).SingleOrDefaultAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(email))
        {
            logger.LogWarning("Skipping notification digest because user {UserId} has no email", userId);
            return;
        }

        tenantContext.TenantId = tenantId;
        tenantContext.UserId = "digest-worker";
        var disabledTypes = await context.NotificationPreferences.AsNoTracking()
            .Where(item => item.UserId == userId && item.Channel == NotificationChannel.InApp && !item.IsEnabled)
            .Select(item => item.Type).ToListAsync(cancellationToken);
        var notifications = await context.Notifications.AsNoTracking()
            .Where(item => item.Channel == NotificationChannel.InApp && item.ResolvedAt == null &&
                           (item.UserId == null || item.UserId == userId) &&
                           item.LastRaisedAt >= start && item.LastRaisedAt < end &&
                           !disabledTypes.Contains(item.Type) &&
                           item.Type != NotificationType.DailyDigest && item.Type != NotificationType.WeeklyDigest)
            .OrderBy(item => item.Type).ThenByDescending(item => item.LastRaisedAt)
            .ToListAsync(cancellationToken);
        var occurrences = notifications.Sum(item => item.OccurrenceCount);
        if (notifications.Count > 0)
        {
            var lines = notifications.GroupBy(item => item.Type).Select(group =>
                $"<li><strong>{WebUtility.HtmlEncode(group.Key.ToString())}</strong>: {group.Sum(item => item.OccurrenceCount)} occurrences across {group.Count()} alerts</li>");
            var body = $"<p>Your InventoryX {label} notification digest for {start:yyyy-MM-dd} to {end.AddTicks(-1):yyyy-MM-dd}.</p><ul>{string.Join(string.Empty, lines)}</ul>";
            await emailSender.SendEmailAsync(email, $"InventoryX {label} notification digest", body);
        }

        context.NotificationDigestDeliveries.Add(new NotificationDigestDelivery
        {
            UserId = userId, DigestType = digestType, PeriodKey = periodKey,
            PeriodStart = start, PeriodEnd = end, ProcessedAt = utcNow,
            NotificationCount = notifications.Count, OccurrenceCount = occurrences,
        });
        await context.SaveChangesAsync(cancellationToken);
    }

    private static (DateTime Start, DateTime End, string Key, string Label) Period(
        NotificationType digestType, DateTime utcNow)
    {
        var today = utcNow.ToUniversalTime().Date;
        if (digestType == NotificationType.DailyDigest)
        {
            var start = today.AddDays(-1);
            return (start, today, $"daily:{start:yyyyMMdd}", "daily");
        }
        var daysSinceMonday = ((int)today.DayOfWeek + 6) % 7;
        var end = today.AddDays(-daysSinceMonday);
        var weeklyStart = end.AddDays(-7);
        return (weeklyStart, end, $"weekly:{weeklyStart:yyyyMMdd}", "weekly");
    }
}
