using InventoryX.Application.Commands.RequestHandlers.Catalog;
using InventoryX.Application.DTOs.Catalog;
using InventoryX.Application.DTOs.Common;
using InventoryX.Application.Exceptions;
using InventoryX.Application.Queries.Requests.Catalog;
using InventoryX.Application.Repository;
using InventoryX.Domain.Models.Catalog;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InventoryX.Application.Queries.RequestHandlers.Catalog
{
    public class GetProductsQueryHandler(IAppDbContext context) : IRequestHandler<GetProductsQuery, PagedResult<ProductDto>>
    {
        public async Task<PagedResult<ProductDto>> Handle(GetProductsQuery request, CancellationToken cancellationToken)
        {
            var query = context.Products
                .Include(p => p.TaxTreatment).Include(p => p.Variants)
                .Where(p => !p.IsDeleted);

            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                // LIKE-based fallback strategy; trigram-style similarity lands with T055.
                var term = $"%{request.Search.Trim()}%";
                query = query.Where(p =>
                    EF.Functions.Like(p.Name, term) ||
                    (p.Sku != null && EF.Functions.Like(p.Sku, term)) ||
                    (p.Barcode != null && EF.Functions.Like(p.Barcode, term)) ||
                    p.Variants.Any(v => (v.Sku != null && EF.Functions.Like(v.Sku, term)) ||
                                        (v.Barcode != null && EF.Functions.Like(v.Barcode, term))));
            }

            if (request.CategoryId is not null) query = query.Where(p => p.CategoryId == request.CategoryId);
            if (!string.IsNullOrEmpty(request.Status) && Enum.TryParse<ProductStatus>(request.Status, true, out var status))
                query = query.Where(p => p.Status == status);
            if (!string.IsNullOrEmpty(request.TrackingMode) && Enum.TryParse<TrackingMode>(request.TrackingMode, true, out var mode))
                query = query.Where(p => p.TrackingMode == mode);
            if (request.BelowReorderPoint == true)
            {
                query = query.Where(p => p.ReorderPoint != null &&
                    context.StockLevels.Where(s => s.ProductId == p.Id).Sum(s => s.QtyOnHand) <= p.ReorderPoint);
            }

            var total = await query.LongCountAsync(cancellationToken);
            var items = await query
                .OrderBy(p => p.Name)
                .Skip(request.Skip).Take(request.PageSize)
                .ToListAsync(cancellationToken);

            return PagedResult<ProductDto>.Create(
                items.Select(p => ProductMapping.ToDto(p, request.IncludeCost)).ToList(),
                request.Page, request.PageSize, total);
        }
    }

    public class GetProductQueryHandler(IAppDbContext context) : IRequestHandler<GetProductQuery, ProductDto>
    {
        public async Task<ProductDto> Handle(GetProductQuery request, CancellationToken cancellationToken)
        {
            var product = await context.Products
                .Include(p => p.TaxTreatment).Include(p => p.Variants)
                .FirstOrDefaultAsync(p => p.Id == request.Id && !p.IsDeleted, cancellationToken)
                ?? throw new NotFoundException("Product not found.");
            return ProductMapping.ToDto(product, request.IncludeCost);
        }
    }

    public class GetProductByBarcodeQueryHandler(IAppDbContext context) : IRequestHandler<GetProductByBarcodeQuery, ProductDto>
    {
        public async Task<ProductDto> Handle(GetProductByBarcodeQuery request, CancellationToken cancellationToken)
        {
            var product = await context.Products
                .Include(p => p.TaxTreatment).Include(p => p.Variants)
                .FirstOrDefaultAsync(p => !p.IsDeleted &&
                    (p.Barcode == request.Barcode || p.Variants.Any(v => v.Barcode == request.Barcode)), cancellationToken)
                ?? throw new NotFoundException("No product matches this barcode.");
            return ProductMapping.ToDto(product, request.IncludeCost);
        }
    }

    public class GetCategoriesQueryHandler(IAppDbContext context) : IRequestHandler<GetCategoriesQuery, List<CategoryDto>>
    {
        public async Task<List<CategoryDto>> Handle(GetCategoriesQuery request, CancellationToken cancellationToken)
        {
            var categories = await context.Categories
                .Where(c => !c.IsDeleted)
                .OrderBy(c => c.Name)
                .ToListAsync(cancellationToken);

            var byParent = categories.ToLookup(c => c.ParentId);
            List<CategoryDto> Build(Guid? parentId) => byParent[parentId]
                .Select(c => new CategoryDto { Id = c.Id, Name = c.Name, ParentId = c.ParentId, Children = Build(c.Id) })
                .ToList();
            return Build(null);
        }
    }

    public class GetTaxTreatmentsQueryHandler(IAppDbContext context) : IRequestHandler<GetTaxTreatmentsQuery, List<TaxTreatmentDto>>
    {
        public async Task<List<TaxTreatmentDto>> Handle(GetTaxTreatmentsQuery request, CancellationToken cancellationToken) =>
            await context.TaxTreatments
                .Where(t => t.IsActive)
                .Select(t => new TaxTreatmentDto
                {
                    Id = t.Id, Code = t.Code, Name = t.Name, CountryCode = t.CountryCode, ComponentsJson = t.ComponentsJson,
                })
                .ToListAsync(cancellationToken);
    }
}
