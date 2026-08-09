using System.Text.Json;
using InventoryX.Application.Behaviors;
using InventoryX.Domain.Models.Auditing;
using MediatR;

namespace InventoryX.Application.Commands.Requests.Reports;

public sealed class CreateReportScheduleCommand : IRequest<ReportScheduleDto>, ITenantWriteCommand
{
    public required string ReportType { get; init; }
    public ReportCadence Cadence { get; init; }
    public required string Format { get; init; }
    public List<string> Recipients { get; init; } = [];
    public Guid? LocationId { get; init; }
    public Guid? CategoryId { get; init; }
    public string? StaffId { get; init; }
}
public sealed record ReportScheduleDto(Guid Id, string ReportType, ReportCadence Cadence, string Format,
    IReadOnlyList<string> Recipients, DateTime NextRunAt, bool IsActive);
public sealed record DeleteReportScheduleCommand(Guid Id) : IRequest<bool>, ITenantWriteCommand;
