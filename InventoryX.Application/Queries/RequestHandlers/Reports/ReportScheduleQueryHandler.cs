using AutoMapper;
using InventoryX.Application.Commands.Requests.Reports;
using InventoryX.Application.DTOs.Common;
using InventoryX.Application.Exceptions;
using InventoryX.Application.Queries.Requests.Reports;
using InventoryX.Application.Repository;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InventoryX.Application.Queries.RequestHandlers.Reports;

public sealed class GetReportSchedulesQueryHandler(IAppDbContext context, IMapper mapper)
    : IRequestHandler<GetReportSchedulesQuery, PagedResult<ReportScheduleDto>>
{
    public async Task<PagedResult<ReportScheduleDto>> Handle(GetReportSchedulesQuery request, CancellationToken cancellationToken)
    {
        var query = context.ReportSchedules.AsNoTracking();
        var totalCount = await query.LongCountAsync(cancellationToken);
        var schedules = await query.OrderBy(schedule => schedule.ReportType).ThenBy(schedule => schedule.NextRunAt)
            .ThenBy(schedule => schedule.Id)
            .Skip(request.Skip).Take(request.PageSize)
            .ToListAsync(cancellationToken);
        return PagedResult<ReportScheduleDto>.Create(
            mapper.Map<IReadOnlyList<ReportScheduleDto>>(schedules), request.Page, request.PageSize, totalCount);
    }
}

public sealed class GetReportScheduleQueryHandler(IAppDbContext context, IMapper mapper)
    : IRequestHandler<GetReportScheduleQuery, ReportScheduleDto>
{
    public async Task<ReportScheduleDto> Handle(GetReportScheduleQuery request, CancellationToken cancellationToken)
    {
        var schedule = await context.ReportSchedules.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Report schedule not found.");
        return mapper.Map<ReportScheduleDto>(schedule);
    }
}
