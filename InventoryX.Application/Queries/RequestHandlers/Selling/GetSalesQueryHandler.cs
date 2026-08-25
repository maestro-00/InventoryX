using InventoryX.Application.Commands.RequestHandlers.Selling;
using InventoryX.Application.DTOs.Common;
using InventoryX.Application.DTOs.Selling;
using InventoryX.Application.Exceptions;
using InventoryX.Application.Queries.Requests.Selling;
using InventoryX.Application.Repository;
using InventoryX.Application.Services.IServices;
using InventoryX.Domain.Models.Selling;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InventoryX.Application.Queries.RequestHandlers.Selling
{
    public class GetSalesQueryHandler(IAppDbContext context, IPosAccess posAccess)
        : IRequestHandler<GetSalesQuery, PagedResult<SaleDto>>
    {
        public async Task<PagedResult<SaleDto>> Handle(GetSalesQuery request, CancellationToken cancellationToken)
        {
            await posAccess.EnsureCanViewSalesAsync(cancellationToken);
            var query = context.Sales
                .Include(s => s.Lines).Include(s => s.Payments)
                .AsQueryable();

            if (request.From is not null) query = query.Where(s => s.OccurredAt >= request.From);
            if (request.To is not null) query = query.Where(s => s.OccurredAt <= request.To);
            if (request.LocationId is not null) query = query.Where(s => s.LocationId == request.LocationId);
            if (request.RegisterId is not null) query = query.Where(s => s.RegisterId == request.RegisterId);
            if (!await posAccess.CanViewOthersAsync(cancellationToken))
                query = query.Where(s => s.CashierId == posAccess.UserId);
            else if (request.CashierId is not null)
                query = query.Where(s => s.CashierId == request.CashierId);
            if (!string.IsNullOrEmpty(request.Status) && Enum.TryParse<SaleStatus>(request.Status, true, out var status))
                query = query.Where(s => s.Status == status);

            var total = await query.LongCountAsync(cancellationToken);
            var sales = await query
                .OrderByDescending(s => s.OccurredAt)
                .Skip(request.Skip).Take(request.PageSize)
                .ToListAsync(cancellationToken);

            return PagedResult<SaleDto>.Create(
                sales.Select(SaleMapping.ToDto).ToList(), request.Page, request.PageSize, total);
        }
    }

    public class GetSaleQueryHandler(IAppDbContext context, IPosAccess posAccess) : IRequestHandler<GetSaleQuery, SaleDto>
    {
        public async Task<SaleDto> Handle(GetSaleQuery request, CancellationToken cancellationToken)
        {
            await posAccess.EnsureCanViewSalesAsync(cancellationToken);
            var sale = await context.Sales
                .Include(s => s.Lines).Include(s => s.Payments)
                .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken)
                ?? throw new NotFoundException("Sale not found.");
            if (!await posAccess.CanViewOthersAsync(cancellationToken)
                && !string.Equals(sale.CashierId, posAccess.UserId, StringComparison.Ordinal))
                throw new NotFoundException("Sale not found.");
            return SaleMapping.ToDto(sale);
        }
    }
}
