using InventoryX.Application.Commands.RequestHandlers.Inventory;
using InventoryX.Application.DTOs.Common;
using InventoryX.Application.DTOs.Inventory;
using InventoryX.Application.Queries.Requests.Inventory;
using InventoryX.Application.Repository;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InventoryX.Application.Queries.RequestHandlers.Inventory
{
    public class GetStockQueryHandler(IAppDbContext context) : IRequestHandler<GetStockQuery, PagedResult<StockLevelDto>>
    {
        public async Task<PagedResult<StockLevelDto>> Handle(GetStockQuery request, CancellationToken cancellationToken)
        {
            var query = context.StockLevels.AsQueryable();
            if (request.LocationId is not null) query = query.Where(s => s.LocationId == request.LocationId);
            if (request.ProductId is not null) query = query.Where(s => s.ProductId == request.ProductId);
            if (request.CategoryId is not null)
                query = query.Where(s => context.Products.Any(p => p.Id == s.ProductId && p.CategoryId == request.CategoryId));
            if (request.BelowReorder == true)
                query = query.Where(s => context.Products.Any(p =>
                    p.Id == s.ProductId && p.ReorderPoint != null && s.QtyOnHand <= p.ReorderPoint));

            if (string.Equals(request.GroupBy, "product", StringComparison.OrdinalIgnoreCase))
            {
                var grouped = query
                    .GroupBy(s => new { s.ProductId, s.VariantId })
                    .Select(g => new
                    {
                        g.Key.ProductId,
                        g.Key.VariantId,
                        QtyOnHand = g.Sum(s => s.QtyOnHand),
                        QtyInTransit = g.Sum(s => s.QtyInTransit),
                        QtyQuarantine = g.Sum(s => s.QtyQuarantine),
                    });
                var totalGrouped = await grouped.LongCountAsync(cancellationToken);
                var rows = await grouped
                    .OrderBy(g => g.ProductId)
                    .Skip(request.Skip).Take(request.PageSize)
                    .ToListAsync(cancellationToken);
                var names = await ProductNamesAsync(rows.Select(r => r.ProductId), cancellationToken);
                return PagedResult<StockLevelDto>.Create(rows.Select(r => new StockLevelDto
                {
                    ProductId = r.ProductId,
                    ProductName = names.GetValueOrDefault(r.ProductId),
                    VariantId = r.VariantId,
                    QtyOnHand = r.QtyOnHand,
                    QtyInTransit = r.QtyInTransit,
                    QtyQuarantine = r.QtyQuarantine,
                }).ToList(), request.Page, request.PageSize, totalGrouped);
            }

            var total = await query.LongCountAsync(cancellationToken);
            var items = await query
                .OrderBy(s => s.ProductId).ThenBy(s => s.LocationId)
                .Skip(request.Skip).Take(request.PageSize)
                .ToListAsync(cancellationToken);
            var productNames = await ProductNamesAsync(items.Select(i => i.ProductId), cancellationToken);

            return PagedResult<StockLevelDto>.Create(items.Select(s => new StockLevelDto
            {
                ProductId = s.ProductId,
                ProductName = productNames.GetValueOrDefault(s.ProductId),
                VariantId = s.VariantId,
                LocationId = s.LocationId,
                BatchId = s.BatchId,
                QtyOnHand = s.QtyOnHand,
                QtyInTransit = s.QtyInTransit,
                QtyQuarantine = s.QtyQuarantine,
                AvgUnitCost = request.IncludeCost ? s.AvgUnitCost : null,
            }).ToList(), request.Page, request.PageSize, total);
        }

        private async Task<Dictionary<Guid, string>> ProductNamesAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken)
        {
            var idList = ids.Distinct().ToList();
            return await context.Products
                .Where(p => idList.Contains(p.Id))
                .Select(p => new { p.Id, p.Name })
                .ToDictionaryAsync(p => p.Id, p => p.Name, cancellationToken);
        }
    }

    public class GetLocationsQueryHandler(IAppDbContext context) : IRequestHandler<GetLocationsQuery, List<LocationDto>>
    {
        public async Task<List<LocationDto>> Handle(GetLocationsQuery request, CancellationToken cancellationToken)
        {
            var locations = await context.Locations
                .Where(l => !l.IsDeleted)
                .OrderBy(l => l.Name)
                .ToListAsync(cancellationToken);
            return locations.Select(LocationMapping.ToDto).ToList();
        }
    }
}
