using InventoryX.Application.DTOs.Selling;
using InventoryX.Application.Queries.Requests.Selling;
using InventoryX.Application.Repository;
using InventoryX.Application.Services.IServices;
using InventoryX.Domain.Models.Selling;
using InventoryX.Domain.Models.Tenancy;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InventoryX.Application.Queries.RequestHandlers.Selling
{
    public class GetShiftsQueryHandler(IAppDbContext context, IPosAccess posAccess)
        : IRequestHandler<GetShiftsQuery, List<ShiftDto>>
    {
        public async Task<List<ShiftDto>> Handle(GetShiftsQuery request, CancellationToken cancellationToken)
        {
            await posAccess.RequireAsync(Permission.Sell, cancellationToken);
            var query = context.Shifts.AsNoTracking().AsQueryable();
            if (!await posAccess.CanViewOthersAsync(cancellationToken))
                query = query.Where(shift => shift.OpenedBy == posAccess.UserId);
            if (request.RegisterId is not null)
                query = query.Where(shift => shift.RegisterId == request.RegisterId);
            if (!string.IsNullOrWhiteSpace(request.Status)
                && Enum.TryParse<ShiftStatus>(request.Status, ignoreCase: true, out var status))
                query = query.Where(shift => shift.Status == status);

            return await query
                .OrderByDescending(shift => shift.OpenedAt)
                .Select(shift => new ShiftDto
                {
                    Id = shift.Id,
                    RegisterId = shift.RegisterId,
                    OpenedBy = shift.OpenedBy,
                    OpenedAt = shift.OpenedAt,
                    OpeningFloat = shift.OpeningFloat,
                    Status = shift.Status.ToString(),
                })
                .ToListAsync(cancellationToken);
        }
    }
}
