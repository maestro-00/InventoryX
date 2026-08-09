using InventoryX.Application.Services.IServices;
using InventoryX.Domain.Models.Tenancy;
using InventoryX.Infrastructure.Data;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace InventoryX.Infrastructure.BackgroundJobs;

public sealed class EmailOutboxWorker(
    IServiceScopeFactory scopes,
    ILogger<EmailOutboxWorker> logger) : BackgroundService
{
    private readonly string _instanceId = $"{Environment.MachineName}:{Guid.NewGuid():N}";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopes.CreateScope();
                await scope.ServiceProvider.GetRequiredService<EmailOutboxProcessor>()
                    .ProcessBatchAsync(_instanceId, DateTime.UtcNow, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception error)
            {
                logger.LogError(error, "Email outbox dispatch failed");
            }
            await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
        }
    }
}

public sealed class EmailOutboxProcessor(
    AppDbContext context,
    IEmailSender emailSender,
    IAttachmentEmailSender attachmentEmailSender,
    ILogger<EmailOutboxProcessor> logger)
{
    private static readonly TimeSpan ClaimLease = TimeSpan.FromMinutes(5);

    public async Task<int> ProcessBatchAsync(
        string instanceId,
        DateTime utcNow,
        CancellationToken cancellationToken,
        int batchSize = 20)
    {
        var processed = 0;
        for (var index = 0; index < batchSize; index++)
        {
            var message = await ClaimNextAsync(instanceId, utcNow, cancellationToken);
            if (message is null) break;
            await DispatchAsync(message, utcNow, cancellationToken);
            processed++;
        }
        return processed;
    }

    internal async Task<OutboxMessage?> ClaimNextAsync(
        string instanceId,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        var candidateIds = await context.OutboxMessages.IgnoreQueryFilters().AsNoTracking()
            .Where(message => message.ProcessedAt == null && message.AvailableAt <= utcNow &&
                              (message.Type == EmailOutbox.HtmlType || message.Type == EmailOutbox.AttachmentType) &&
                              (message.ClaimExpiresAt == null || message.ClaimExpiresAt <= utcNow))
            .OrderBy(message => message.AvailableAt).ThenBy(message => message.OccurredAt).ThenBy(message => message.Id)
            .Select(message => message.Id).Take(10).ToListAsync(cancellationToken);

        foreach (var candidateId in candidateIds)
        {
            var claimed = await context.OutboxMessages.IgnoreQueryFilters()
                .Where(message => message.Id == candidateId && message.ProcessedAt == null &&
                                  message.AvailableAt <= utcNow &&
                                  (message.ClaimExpiresAt == null || message.ClaimExpiresAt <= utcNow))
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(message => message.ClaimedBy, instanceId)
                    .SetProperty(message => message.ClaimExpiresAt, utcNow.Add(ClaimLease)), cancellationToken);
            if (claimed == 0) continue;

            context.ChangeTracker.Clear();
            return await context.OutboxMessages.IgnoreQueryFilters()
                .SingleAsync(message => message.Id == candidateId, cancellationToken);
        }

        return null;
    }

    private async Task DispatchAsync(
        OutboxMessage message,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        try
        {
            var payload = EmailOutbox.Deserialize(message);
            if (message.Type == EmailOutbox.AttachmentType)
            {
                await attachmentEmailSender.SendAsync(
                    payload.To, payload.Subject, payload.HtmlBody,
                    payload.FileName ?? throw new InvalidOperationException("Attachment file name is required."),
                    payload.ContentType ?? "application/octet-stream", payload.Content ?? [], cancellationToken);
            }
            else
            {
                await emailSender.SendEmailAsync(payload.To, payload.Subject, payload.HtmlBody);
            }

            message.ProcessedAt = utcNow;
            message.ClaimedBy = null;
            message.ClaimExpiresAt = null;
            message.LastError = null;
        }
        catch (Exception error)
        {
            message.AttemptCount++;
            message.LastError = error.Message.Length <= 2000 ? error.Message : error.Message[..2000];
            message.AvailableAt = utcNow.Add(Backoff(message.AttemptCount));
            message.ClaimedBy = null;
            message.ClaimExpiresAt = null;
            logger.LogWarning(error, "Email outbox message {MessageId} failed on attempt {AttemptCount}",
                message.Id, message.AttemptCount);
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    internal static TimeSpan Backoff(int attemptCount) =>
        TimeSpan.FromMinutes(Math.Min(Math.Pow(2, Math.Clamp(attemptCount, 1, 6)), 60));
}
