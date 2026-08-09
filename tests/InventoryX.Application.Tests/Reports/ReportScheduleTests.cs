using AutoMapper;
using FluentAssertions;
using InventoryX.Application.Commands.RequestHandlers.Reports;
using InventoryX.Application.Commands.Requests.Reports;
using InventoryX.Application.Extensions;
using InventoryX.Application.Queries.RequestHandlers.Reports;
using InventoryX.Application.Queries.Requests.Reports;
using InventoryX.Application.Exceptions;
using InventoryX.Common.Tests;
using InventoryX.Domain.Models.Auditing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace InventoryX.Application.Tests.Reports;

public sealed class ReportScheduleTests : IDisposable
{
    private readonly TestDb _db = new(Guid.NewGuid());
    private readonly IMapper _mapper = new MapperConfiguration(
        configuration => configuration.AddProfile<ReportScheduleMappingProfile>(),
        NullLoggerFactory.Instance).CreateMapper();

    [Fact]
    public async Task Create_schedule_persists_cadence_format_recipients_and_next_run()
    {
        await using var context = _db.CreateContext();
        var result = await new CreateReportScheduleCommandHandler(context, _mapper).Handle(new CreateReportScheduleCommand
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
        await new CreateReportScheduleCommandHandler(context, _mapper).Handle(new CreateReportScheduleCommand
        {
            ReportType = "tax", Cadence = ReportCadence.Monthly, Format = "pdf",
            Recipients = ["finance@example.com"],
        }, CancellationToken.None);

        var schedules = await new GetReportSchedulesQueryHandler(context, _mapper)
            .Handle(new GetReportSchedulesQuery(), CancellationToken.None);

        schedules.Should().ContainSingle(schedule => schedule.ReportType == "tax" && schedule.Format == "pdf");
    }

    [Fact]
    public async Task Create_schedule_rejects_unsupported_report_type()
    {
        await using var context = _db.CreateContext();
        var action = () => new CreateReportScheduleCommandHandler(context, _mapper).Handle(new CreateReportScheduleCommand
        {
            ReportType = "unknown", Cadence = ReportCadence.Daily, Format = "csv",
            Recipients = ["owner@example.com"],
        }, CancellationToken.None);

        await action.Should().ThrowAsync<FluentValidation.ValidationException>();
    }

    [Fact]
    public async Task Get_schedule_returns_requested_schedule()
    {
        await using var context = _db.CreateContext();
        var created = await new CreateReportScheduleCommandHandler(context, _mapper).Handle(new CreateReportScheduleCommand
        {
            ReportType = "sales", Cadence = ReportCadence.Daily, Format = "csv",
            Recipients = ["owner@example.com"],
        }, CancellationToken.None);

        var result = await new GetReportSchedulesQueryHandler(context, _mapper)
            .Handle(new GetReportSchedulesQuery(created.Id), CancellationToken.None);

        result.Should().ContainSingle(schedule => schedule.Id == created.Id);
    }

    [Fact]
    public async Task Get_schedule_rejects_unknown_id()
    {
        await using var context = _db.CreateContext();
        var action = () => new GetReportSchedulesQueryHandler(context, _mapper)
            .Handle(new GetReportSchedulesQuery(Guid.NewGuid()), CancellationToken.None);

        await action.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Delete_schedule_deactivates_existing_schedule()
    {
        await using var context = _db.CreateContext();
        var created = await new CreateReportScheduleCommandHandler(context, _mapper).Handle(new CreateReportScheduleCommand
        {
            ReportType = "stock", Cadence = ReportCadence.Monthly, Format = "pdf",
            Recipients = ["owner@example.com"],
        }, CancellationToken.None);

        var deleted = await new DeleteReportScheduleCommandHandler(context)
            .Handle(new DeleteReportScheduleCommand(created.Id), CancellationToken.None);

        deleted.Should().BeTrue();
        (await context.ReportSchedules.SingleAsync()).IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Delete_schedule_rejects_unknown_id()
    {
        await using var context = _db.CreateContext();
        var action = () => new DeleteReportScheduleCommandHandler(context)
            .Handle(new DeleteReportScheduleCommand(Guid.NewGuid()), CancellationToken.None);

        await action.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public void Mapping_profile_is_valid_and_handles_malformed_recipient_json()
    {
        var configuration = new MapperConfiguration(
            expression => expression.AddProfile<ReportScheduleMappingProfile>(),
            NullLoggerFactory.Instance);
        configuration.AssertConfigurationIsValid();
        var schedule = new ReportSchedule
        {
            ReportType = "sales",
            Format = "csv",
            RecipientsJson = "not-json",
            NextRunAt = DateTime.UtcNow,
        };

        configuration.CreateMapper().Map<ReportScheduleDto>(schedule).Recipients.Should().BeEmpty();
    }

    public void Dispose() => _db.Dispose();
}
