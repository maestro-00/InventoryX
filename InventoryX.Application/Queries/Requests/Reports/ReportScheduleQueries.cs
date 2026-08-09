using InventoryX.Application.Commands.Requests.Reports;
using InventoryX.Application.DTOs.Common;
using MediatR;

namespace InventoryX.Application.Queries.Requests.Reports;

public sealed record GetReportSchedulesQuery : PageRequest, IRequest<PagedResult<ReportScheduleDto>>;

public sealed record GetReportScheduleQuery(Guid Id) : IRequest<ReportScheduleDto>;
