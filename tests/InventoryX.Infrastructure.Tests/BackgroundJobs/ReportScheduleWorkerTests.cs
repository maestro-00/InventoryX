using System.Text.Json;
using FluentAssertions;
using InventoryX.Application.Queries.Requests.Reports;
using InventoryX.Application.Repository;
using InventoryX.Application.Services.IServices;
using InventoryX.Common.Tests;
using InventoryX.Domain.Models.Auditing;
using InventoryX.Infrastructure.BackgroundJobs;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace InventoryX.Infrastructure.Tests.BackgroundJobs;

public sealed class ReportScheduleWorkerTests : IDisposable
{
    private sealed class RecordingMailer(string? failingRecipient = null) : IAttachmentEmailSender
    {
        public List<string> Recipients { get; } = [];
        public List<string> FileNames { get; } = [];

        public Task SendAsync(
            string to,
            string subject,
            string htmlBody,
            string fileName,
            string contentType,
            byte[] content,
            CancellationToken cancellationToken = default)
        {
            if (to == failingRecipient) throw new InvalidOperationException("Delivery failed.");
            Recipients.Add(to);
            FileNames.Add(fileName);
            return Task.CompletedTask;
        }
    }

    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly TestDb _db;

    public ReportScheduleWorkerTests()
    {
        _db = new TestDb(_tenantId, "owner-1");
    }

    [Fact]
    public async Task Due_weekly_schedule_exports_emails_every_recipient_and_advances()
    {
        await using var context = _db.CreateContext();
        var dueAt = DateTime.UtcNow.AddMinutes(-5);
        var schedule = NewSchedule("sales", ReportCadence.Weekly, dueAt,
            "owner@example.com", "manager@example.com");
        context.ReportSchedules.Add(schedule);
        await context.SaveChangesAsync();

        ExportReportCommand? export = null;
        var sender = new Mock<ISender>();
        sender.Setup(item => item.Send(It.IsAny<ExportReportCommand>(), It.IsAny<CancellationToken>()))
            .Callback<object, CancellationToken>((request, _) => export = (ExportReportCommand)request)
            .ReturnsAsync(new ReportExportResult(null, ReportExportStatus.Completed, false,
                "sales.xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", [1, 2, 3]));
        var mailer = new RecordingMailer();
        using var services = Services(context, sender.Object, mailer);
        var worker = new ReportScheduleWorker(
            services.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<ReportScheduleWorker>.Instance);

        await worker.RunDueAsync(CancellationToken.None);

        export.Should().NotBeNull();
        export!.ReportType.Should().Be("sales");
        export.Format.Should().Be("xlsx");
        (export.Filter.To - export.Filter.From).Should().Be(TimeSpan.FromDays(7));
        mailer.Recipients.Should().BeEquivalentTo("owner@example.com", "manager@example.com");
        mailer.FileNames.Should().OnlyContain(fileName => fileName == "sales.xlsx");
        schedule.LastRunAt.Should().NotBeNull();
        schedule.NextRunAt.Should().BeAfter(schedule.LastRunAt!.Value);
        _db.TenantContext.TenantId.Should().Be(_tenantId);
        _db.TenantContext.UserId.Should().Be("report-schedule-worker");
    }

    [Fact]
    public async Task Failed_delivery_does_not_advance_and_does_not_block_another_schedule()
    {
        await using var context = _db.CreateContext();
        var dueAt = DateTime.UtcNow.AddMinutes(-5);
        var failed = NewSchedule("sales", ReportCadence.Weekly, dueAt, "fail@example.com");
        var succeeded = NewSchedule("stock", ReportCadence.Weekly, dueAt, "ok@example.com");
        context.ReportSchedules.AddRange(failed, succeeded);
        await context.SaveChangesAsync();

        var sender = new Mock<ISender>();
        sender.Setup(item => item.Send(It.IsAny<ExportReportCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ExportReportCommand request, CancellationToken _) =>
                new ReportExportResult(null, ReportExportStatus.Completed, false,
                    $"{request.ReportType}.csv", "text/csv", [1]));
        var mailer = new RecordingMailer("fail@example.com");
        using var services = Services(context, sender.Object, mailer);
        var worker = new ReportScheduleWorker(
            services.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<ReportScheduleWorker>.Instance);

        await worker.RunDueAsync(CancellationToken.None);

        failed.LastRunAt.Should().BeNull();
        failed.NextRunAt.Should().Be(dueAt);
        succeeded.LastRunAt.Should().NotBeNull();
        succeeded.NextRunAt.Should().BeAfter(succeeded.LastRunAt!.Value);
        mailer.Recipients.Should().ContainSingle().Which.Should().Be("ok@example.com");
    }

    private ReportSchedule NewSchedule(
        string reportType,
        ReportCadence cadence,
        DateTime nextRunAt,
        params string[] recipients) => new()
    {
        TenantId = _tenantId,
        ReportType = reportType,
        Cadence = cadence,
        Format = reportType == "sales" ? "xlsx" : "csv",
        RecipientsJson = JsonSerializer.Serialize(recipients),
        NextRunAt = nextRunAt,
    };

    private ServiceProvider Services(
        IAppDbContext context,
        ISender sender,
        IAttachmentEmailSender mailer)
    {
        var collection = new ServiceCollection();
        collection.AddSingleton(context);
        collection.AddSingleton<IAppDbContext>(context);
        collection.AddSingleton<ITenantContext>(_db.TenantContext);
        collection.AddSingleton(sender);
        collection.AddSingleton<ISender>(sender);
        collection.AddSingleton(mailer);
        collection.AddSingleton<IAttachmentEmailSender>(mailer);
        return collection.BuildServiceProvider();
    }

    public void Dispose() => _db.Dispose();
}
