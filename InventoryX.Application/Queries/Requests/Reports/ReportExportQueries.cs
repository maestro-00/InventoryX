using InventoryX.Application.Behaviors;
using InventoryX.Application.Services.IServices;
using InventoryX.Domain.Models.Auditing;
using MediatR;

namespace InventoryX.Application.Queries.Requests.Reports;

public sealed record ExportReportCommand(string ReportType, string Format, ReportFilter Filter)
    : IRequest<ReportExportResult>, IReadOnlyWriteExemptCommand;
public sealed record GetReportExportJobQuery(Guid JobId) : IRequest<ReportExportResult>;
public sealed record ReportExportResult(Guid? JobId, ReportExportStatus Status, bool Accepted,
    string? FileName = null, string? ContentType = null, byte[]? Content = null, string? Error = null);
