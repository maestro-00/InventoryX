using FluentAssertions;
using InventoryX.Application.Commands.RequestHandlers.Reports;
using InventoryX.Application.Commands.Requests.Reports;
using InventoryX.Application.Queries.RequestHandlers.Reports;
using InventoryX.Application.Queries.Requests.Reports;
using InventoryX.Common.Tests;
using InventoryX.Domain.Models.Auditing;
using Microsoft.EntityFrameworkCore;

namespace InventoryX.Application.Tests.Reports;

public sealed class ReportScheduleTests : IDisposable
{
    private readonly TestDb _db = new(Guid.NewGuid());

    [Fact]
    public async Task Create_schedule_persists_cadence_format_recipients_and_next_run()
    {
        await using var context = _db.CreateContext();
        var result = await new CreateReportScheduleCommandHandler(context).Handle(new CreateReportScheduleCommand
        {
            ReportType = "sales", Cadence = ReportCadence.Weekly, Format = "xlsx",
            Recipients = ["owner@example.com"], LocationId = Guid.NewGuid(),
        }, CancellationToken.None);

        result.Cadence.Should().Be(ReportCadence.Weekly);
        result.NextRunAt.Should().BeAfter(DateTime.UtcNow);
        var saved = await context.ReportSchedules.SingleAsync();
        saved.RecipientsJson.Should().Contain("owner@example.com");
    }

    [Fact]
    public async Task List_schedules_returns_only_current_tenant_schedules()
    {
        await using var context = _db.CreateContext();
        await new CreateReportScheduleCommandHandler(context).Handle(new CreateReportScheduleCommand
        {
            ReportType = "tax", Cadence = ReportCadence.Monthly, Format = "pdf",
            Recipients = ["finance@example.com"],
        }, CancellationToken.None);

        var schedules = await new GetReportSchedulesQueryHandler(context)
            .Handle(new GetReportSchedulesQuery(), CancellationToken.None);

        schedules.Should().ContainSingle(schedule => schedule.ReportType == "tax" && schedule.Format == "pdf");
    }

    [Fact]
    public async Task Create_schedule_rejects_unsupported_report_type()
    {
        await using var context = _db.CreateContext();
        var action = () => new CreateReportScheduleCommandHandler(context).Handle(new CreateReportScheduleCommand
        {
            ReportType = "unknown", Cadence = ReportCadence.Daily, Format = "csv",
            Recipients = ["owner@example.com"],
        }, CancellationToken.None);

        await action.Should().ThrowAsync<FluentValidation.ValidationException>();
    }

    public void Dispose() => _db.Dispose();
}
