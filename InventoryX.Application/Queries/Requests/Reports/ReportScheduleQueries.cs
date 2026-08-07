using InventoryX.Application.Commands.Requests.Reports;
using MediatR;

namespace InventoryX.Application.Queries.Requests.Reports;

public sealed record GetReportSchedulesQuery(Guid? Id = null) : IRequest<IReadOnlyList<ReportScheduleDto>>;
