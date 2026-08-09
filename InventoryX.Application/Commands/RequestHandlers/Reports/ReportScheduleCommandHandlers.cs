using AutoMapper;
using System.Text.Json;
using System.Net.Mail;
using InventoryX.Application.Commands.Requests.Reports;
using InventoryX.Application.Exceptions;
using InventoryX.Application.Repository;
using InventoryX.Domain.Models.Auditing;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InventoryX.Application.Commands.RequestHandlers.Reports;

public sealed class CreateReportScheduleCommandHandler(IAppDbContext context, IMapper mapper)
    : IRequestHandler<CreateReportScheduleCommand, ReportScheduleDto>
{
    private static readonly HashSet<string> SupportedReports =
        ["sales", "profit", "stock", "purchasing", "staff", "tax"];

    public async Task<ReportScheduleDto> Handle(CreateReportScheduleCommand request, CancellationToken cancellationToken)
    {
        if (request.Recipients.Count == 0 || request.Recipients.Any(recipient => !MailAddress.TryCreate(recipient, out _)))
            throw new FluentValidation.ValidationException("At least one valid report recipient is required.");
        var reportType = request.ReportType.Trim().ToLowerInvariant();
        var format = request.Format.Trim().ToLowerInvariant();
        if (!SupportedReports.Contains(reportType))
            throw new FluentValidation.ValidationException("Report type is invalid.");
        if (!Enum.IsDefined(request.Cadence) || !new[] { "csv", "xlsx", "pdf" }.Contains(format))
            throw new FluentValidation.ValidationException("Cadence or format is invalid.");
        var schedule = new ReportSchedule
        {
            ReportType = reportType, Cadence = request.Cadence, Format = format,
            RecipientsJson = JsonSerializer.Serialize(request.Recipients.Select(recipient => recipient.Trim()).Distinct(StringComparer.OrdinalIgnoreCase)),
            LocationId = request.LocationId, CategoryId = request.CategoryId, StaffId = request.StaffId,
            NextRunAt = CalculateNextRun(DateTime.UtcNow, request.Cadence),
        };
        context.ReportSchedules.Add(schedule);
        await context.SaveChangesAsync(cancellationToken);
        return mapper.Map<ReportScheduleDto>(schedule);
    }

    public static DateTime CalculateNextRun(DateTime from, ReportCadence cadence) => cadence switch
    { ReportCadence.Daily => from.Date.AddDays(1), ReportCadence.Weekly => from.Date.AddDays(7), ReportCadence.Monthly => from.Date.AddMonths(1), _ => from.AddDays(1) };
}

public sealed class DeleteReportScheduleCommandHandler(IAppDbContext context) : IRequestHandler<DeleteReportScheduleCommand, bool>
{
    public async Task<bool> Handle(DeleteReportScheduleCommand request, CancellationToken cancellationToken)
    {
        var schedule = await context.ReportSchedules.SingleOrDefaultAsync(item => item.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Report schedule not found.");
        schedule.IsActive = false;
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
