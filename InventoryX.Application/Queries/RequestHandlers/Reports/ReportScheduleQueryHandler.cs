using AutoMapper;
using InventoryX.Application.Commands.Requests.Reports;
using InventoryX.Application.Exceptions;
using InventoryX.Application.Queries.Requests.Reports;
using InventoryX.Application.Repository;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InventoryX.Application.Queries.RequestHandlers.Reports;

public sealed class GetReportSchedulesQueryHandler(IAppDbContext context, IMapper mapper)
    : IRequestHandler<GetReportSchedulesQuery, IReadOnlyList<ReportScheduleDto>>
{
    public async Task<IReadOnlyList<ReportScheduleDto>> Handle(GetReportSchedulesQuery request, CancellationToken cancellationToken)
    {
        var query = context.ReportSchedules.AsNoTracking();
        if (request.Id is not null) query = query.Where(schedule => schedule.Id == request.Id);
        var schedules = await query.OrderBy(schedule => schedule.ReportType).ThenBy(schedule => schedule.NextRunAt)
            .ToListAsync(cancellationToken);
        if (request.Id is not null && schedules.Count == 0) throw new NotFoundException("Report schedule not found.");
        return mapper.Map<IReadOnlyList<ReportScheduleDto>>(schedules);
    }
}
