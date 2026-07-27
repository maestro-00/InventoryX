using InventoryX.Application.DTOs.Selling;
using InventoryX.Application.Queries.Requests.Selling;
using InventoryX.Application.Repository;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InventoryX.Application.Queries.RequestHandlers.Selling
{
    public class GetRegistersQueryHandler(IAppDbContext context) : IRequestHandler<GetRegistersQuery, List<RegisterDto>>
    {
        public async Task<List<RegisterDto>> Handle(GetRegistersQuery request, CancellationToken cancellationToken)
        {
            var query = context.Registers.AsQueryable();
            if (request.LocationId is not null) query = query.Where(r => r.LocationId == request.LocationId);
            return await query
                .OrderBy(r => r.Name)
                .Select(r => new RegisterDto
                {
                    Id = r.Id,
                    LocationId = r.LocationId,
                    Name = r.Name,
                    IsActive = r.IsActive,
                })
                .ToListAsync(cancellationToken);
        }
    }
}
