using InventoryX.Application.Commands.RequestHandlers.Selling;
using InventoryX.Application.DTOs.Selling;
using InventoryX.Application.Exceptions;
using InventoryX.Application.Queries.Requests.Selling;
using InventoryX.Application.Repository;
using InventoryX.Domain.Models.Selling;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InventoryX.Application.Queries.RequestHandlers.Selling
{
    public class GetHeldSalesQueryHandler(IAppDbContext context)
        : IRequestHandler<GetHeldSalesQuery, List<SaleDto>>
    {
        public async Task<List<SaleDto>> Handle(GetHeldSalesQuery request, CancellationToken cancellationToken) =>
            (await context.Sales
                .Include(s => s.Lines).Include(s => s.Payments)
                .Where(s => s.Status == SaleStatus.Held)
                .OrderByDescending(s => s.OccurredAt)
                .ToListAsync(cancellationToken))
            .Select(SaleMapping.ToDto).ToList();
    }

    public class GetHeldSaleQueryHandler(IAppDbContext context) : IRequestHandler<GetHeldSaleQuery, SaleDto>
    {
        public async Task<SaleDto> Handle(GetHeldSaleQuery request, CancellationToken cancellationToken)
        {
            var sale = await context.Sales.Include(s => s.Lines).Include(s => s.Payments)
                .FirstOrDefaultAsync(s => s.Id == request.Id && s.Status == SaleStatus.Held, cancellationToken)
                ?? throw new NotFoundException("Held sale not found.");
            return SaleMapping.ToDto(sale);
        }
    }

    public class GetFavouritesLayoutQueryHandler(IAppDbContext context)
        : IRequestHandler<GetFavouritesLayoutQuery, FavouritesLayoutDto>
    {
        public async Task<FavouritesLayoutDto> Handle(
            GetFavouritesLayoutQuery request,
            CancellationToken cancellationToken)
        {
            if (!await context.Registers.AnyAsync(r => r.Id == request.RegisterId && r.IsActive, cancellationToken))
                throw new NotFoundException("Register not found.");
            var layout = await context.FavouritesLayouts
                .FirstOrDefaultAsync(f => f.RegisterId == request.RegisterId, cancellationToken);
            return new FavouritesLayoutDto(request.RegisterId, layout?.LayoutJson ?? "{}");
        }
    }
}
