using InventoryX.Application.Commands.RequestHandlers.Selling;
using InventoryX.Application.DTOs.Selling;
using InventoryX.Application.Exceptions;
using InventoryX.Application.Queries.Requests.Selling;
using InventoryX.Application.Repository;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InventoryX.Application.Queries.RequestHandlers.Selling
{
    public class LookupSaleForReturnQueryHandler(IAppDbContext context)
        : IRequestHandler<LookupSaleForReturnQuery, List<SaleDto>>
    {
        public async Task<List<SaleDto>> Handle(LookupSaleForReturnQuery request, CancellationToken cancellationToken)
        {
            var query = context.Sales.Include(s => s.Lines).Include(s => s.Payments).AsQueryable();

            if (!string.IsNullOrWhiteSpace(request.ReceiptNumber))
            {
                var receiptSaleIds = context.Receipts
                    .Where(r => r.Number == request.ReceiptNumber)
                    .Select(r => r.SaleId);
                query = query.Where(s => receiptSaleIds.Contains(s.Id));
            }
            else if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var term = request.Search.Trim();
                // Match receipt number or the client-visible sale id prefix.
                var matchingReceipts = context.Receipts
                    .Where(r => r.Number.Contains(term))
                    .Select(r => r.SaleId);
                query = query.Where(s => matchingReceipts.Contains(s.Id)
                    || s.ClientSaleId.ToString().Contains(term));
            }
            else
            {
                throw new FluentValidation.ValidationException(
                    "Provide a receiptNumber or a search term to look up a sale.");
            }

            var sales = await query
                .OrderByDescending(s => s.OccurredAt)
                .Take(20)
                .ToListAsync(cancellationToken);
            return sales.Select(SaleMapping.ToDto).ToList();
        }
    }

    public class GetProductAvailabilityQueryHandler(IAppDbContext context)
        : IRequestHandler<GetProductAvailabilityQuery, ProductAvailabilityDto>
    {
        public async Task<ProductAvailabilityDto> Handle(GetProductAvailabilityQuery request, CancellationToken cancellationToken)
        {
            var product = await context.Products
                .FirstOrDefaultAsync(p => p.Id == request.ProductId && !p.IsDeleted, cancellationToken)
                ?? throw new NotFoundException("Product not found.");

            var levels = context.StockLevels.Where(l => l.ProductId == request.ProductId);
            if (request.VariantId is not null) levels = levels.Where(l => l.VariantId == request.VariantId);
            if (request.LocationId is not null) levels = levels.Where(l => l.LocationId == request.LocationId);

            var onHand = await levels.SumAsync(l => l.QtyOnHand, cancellationToken);
            return new ProductAvailabilityDto
            {
                ProductId = product.Id,
                VariantId = request.VariantId,
                ProductName = product.Name,
                LocationId = request.LocationId,
                QtyOnHand = onHand,
                QtyAvailable = onHand,
                InStock = onHand > 0,
            };
        }
    }
}
