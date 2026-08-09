using System.Text.Json;
using InventoryX.Application.Commands.RequestHandlers.Reports;
using InventoryX.Application.Queries.Requests.Reports;
using InventoryX.Application.Repository;
using InventoryX.Application.Services.IServices;
using InventoryX.Domain.Models.Auditing;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace InventoryX.Infrastructure.BackgroundJobs;

public sealed class ReportScheduleWorker(IServiceScopeFactory scopes, ILogger<ReportScheduleWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try { await RunDueAsync(stoppingToken); }
            catch (Exception error) { logger.LogError(error, "Report schedule scan failed"); }
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }

    internal async Task RunDueAsync(CancellationToken cancellationToken)
    {
        using var scope = scopes.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var tenantContext = scope.ServiceProvider.GetRequiredService<ITenantContext>();
        var due = await context.ReportSchedules
            .IgnoreQueryFilters()
            .Where(schedule => schedule.IsActive && schedule.NextRunAt <= DateTime.UtcNow)
            .ToListAsync(cancellationToken);
        foreach (var schedule in due)
        {
            try
            {
                tenantContext.TenantId = schedule.TenantId;
                tenantContext.UserId = "report-schedule-worker";
                var to = DateTime.UtcNow;
                var from = schedule.Cadence switch
                {
                    ReportCadence.Daily => to.AddDays(-1),
                    ReportCadence.Weekly => to.AddDays(-7),
                    ReportCadence.Monthly => to.AddMonths(-1),
                    _ => to.AddDays(-1),
                };
                var report = await sender.Send(new ExportReportCommand(schedule.ReportType, schedule.Format,
                    new ReportFilter(from, to, schedule.LocationId, schedule.CategoryId, schedule.StaffId)), cancellationToken);
                if (report.Content is null || report.FileName is null || report.ContentType is null)
                    throw new InvalidOperationException("Scheduled report export did not produce an attachment.");
                foreach (var recipient in JsonSerializer.Deserialize<List<string>>(schedule.RecipientsJson) ?? [])
                    context.OutboxMessages.Add(EmailOutbox.Attachment(
                        schedule.TenantId,
                        $"report:{schedule.Id}:{to:O}:{recipient.Trim().ToLowerInvariant()}",
                        recipient,
                        $"InventoryX {schedule.ReportType} report",
                        "<p>Your scheduled report is attached.</p>",
                        report.FileName,
                        report.ContentType,
                        report.Content,
                        to));
                schedule.LastRunAt = to;
                schedule.NextRunAt = CreateReportScheduleCommandHandler.CalculateNextRun(to, schedule.Cadence);
                await context.SaveChangesAsync(cancellationToken);
            }
            catch (Exception error)
            {
                logger.LogError(error, "Scheduled report {ScheduleId} failed for tenant {TenantId}", schedule.Id, schedule.TenantId);
            }
        }
    }
}
