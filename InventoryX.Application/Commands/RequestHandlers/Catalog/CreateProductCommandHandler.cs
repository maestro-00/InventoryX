using System.Text.Json;
using InventoryX.Application.Commands.Requests.Catalog;
using InventoryX.Application.DTOs.Catalog;
using InventoryX.Application.Exceptions;
using InventoryX.Application.Repository;
using InventoryX.Application.Services;
using InventoryX.Domain.Models.Catalog;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InventoryX.Application.Commands.RequestHandlers.Catalog
{
    public static class ProductMapping
    {
        public static ProductDto ToDto(Product product, bool includeCost = true) => new()
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            Sku = product.Sku,
            Barcode = product.Barcode,
            CategoryId = product.CategoryId,
            UnitOfMeasure = product.UnitOfMeasure.ToString(),
            AllowFractional = product.AllowFractional,
            SellingPrice = product.SellingPrice,
            CostPrice = includeCost ? product.CostPrice : null,
            TaxTreatmentCode = product.TaxTreatment?.Code,
            TrackingMode = product.TrackingMode.ToString(),
            Status = product.Status.ToString(),
            ReorderPoint = product.ReorderPoint,
            ReorderQuantity = product.ReorderQuantity,
            LeadTimeDays = product.LeadTimeDays,
            VariantAttributes = string.IsNullOrEmpty(product.VariantAttributes)
                ? []
                : JsonSerializer.Deserialize<List<string>>(product.VariantAttributes) ?? [],
            Variants = product.Variants.Where(v => !v.IsDeleted).Select(v => new ProductVariantDto
            {
                Id = v.Id,
                AttributeValues = JsonSerializer.Deserialize<Dictionary<string, string>>(v.AttributeValues) ?? [],
                Sku = v.Sku,
                Barcode = v.Barcode,
                SellingPrice = v.SellingPrice,
                CostPrice = includeCost ? v.CostPrice : null,
            }).ToList(),
            RowVersion = product.RowVersion,
        };
    }

    public class CreateProductCommandHandler(IAppDbContext context) : IRequestHandler<CreateProductCommand, ProductDto>
    {
        public async Task<ProductDto> Handle(CreateProductCommand request, CancellationToken cancellationToken)
        {
            if (!string.IsNullOrWhiteSpace(request.Sku) &&
                await context.Products.AnyAsync(p => p.Sku == request.Sku && !p.IsDeleted, cancellationToken))
                throw new ConflictException($"A product with SKU '{request.Sku}' already exists.");

            TaxTreatment? tax = null;
            if (!string.IsNullOrWhiteSpace(request.TaxTreatmentCode))
            {
                tax = await context.TaxTreatments.FirstOrDefaultAsync(t => t.Code == request.TaxTreatmentCode, cancellationToken)
                    ?? throw new NotFoundException($"Tax treatment '{request.TaxTreatmentCode}' does not exist.");
            }

            var product = new Product
            {
                Name = request.Name,
                Description = request.Description,
                Sku = request.Sku,
                Barcode = request.Barcode,
                CategoryId = request.CategoryId,
                UnitOfMeasure = Enum.TryParse<UnitOfMeasure>(request.UnitOfMeasure, true, out var uom) ? uom : UnitOfMeasure.Each,
                AllowFractional = request.AllowFractional,
                SellingPrice = request.SellingPrice,
                CostPrice = request.CostPrice,
                TaxTreatmentId = tax?.Id,
                TaxTreatment = tax,
                TrackingMode = Enum.TryParse<TrackingMode>(request.TrackingMode, true, out var mode) ? mode : TrackingMode.Simple,
                VariantAttributes = request.VariantAttributes is { Count: > 0 }
                    ? JsonSerializer.Serialize(request.VariantAttributes)
                    : null,
                ReorderPoint = request.ReorderPoint,
                ReorderQuantity = request.ReorderQuantity,
                LeadTimeDays = request.LeadTimeDays,
                IsSampleData = request.IsSampleData,
            };
            context.Products.Add(product);
            await context.SaveChangesAsync(cancellationToken);
            return ProductMapping.ToDto(product);
        }
    }

    public class UpdateProductCommandHandler(IAppDbContext context) : IRequestHandler<UpdateProductCommand, ProductDto>
    {
        public async Task<ProductDto> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
        {
            var product = await context.Products
                .Include(p => p.TaxTreatment).Include(p => p.Variants)
                .FirstOrDefaultAsync(p => p.Id == request.Id && !p.IsDeleted, cancellationToken)
                ?? throw new NotFoundException("Product not found.");

            RowVersionGuard.EnsureMatch(product.RowVersion, request.ExpectedRowVersion);

            if (request.Sku is not null && request.Sku != product.Sku &&
                await context.Products.AnyAsync(p => p.Sku == request.Sku && p.Id != product.Id && !p.IsDeleted, cancellationToken))
                throw new ConflictException($"A product with SKU '{request.Sku}' already exists.");

            product.Name = request.Name ?? product.Name;
            product.Description = request.Description ?? product.Description;
            product.Sku = request.Sku ?? product.Sku;
            product.Barcode = request.Barcode ?? product.Barcode;
            product.CategoryId = request.CategoryId ?? product.CategoryId;
            product.SellingPrice = request.SellingPrice ?? product.SellingPrice;
            product.CostPrice = request.CostPrice ?? product.CostPrice;
            product.ReorderPoint = request.ReorderPoint ?? product.ReorderPoint;
            product.ReorderQuantity = request.ReorderQuantity ?? product.ReorderQuantity;
            product.LeadTimeDays = request.LeadTimeDays ?? product.LeadTimeDays;
            if (request.Status is not null && Enum.TryParse<ProductStatus>(request.Status, true, out var status))
                product.Status = status;
            if (!string.IsNullOrWhiteSpace(request.TaxTreatmentCode))
            {
                var tax = await context.TaxTreatments.FirstOrDefaultAsync(t => t.Code == request.TaxTreatmentCode, cancellationToken)
                    ?? throw new NotFoundException($"Tax treatment '{request.TaxTreatmentCode}' does not exist.");
                product.TaxTreatmentId = tax.Id;
                product.TaxTreatment = tax;
            }

            await context.SaveChangesAsync(cancellationToken);
            return ProductMapping.ToDto(product);
        }
    }

    public class AddProductVariantsCommandHandler(IAppDbContext context) : IRequestHandler<AddProductVariantsCommand, ProductDto>
    {
        public async Task<ProductDto> Handle(AddProductVariantsCommand request, CancellationToken cancellationToken)
        {
            var product = await context.Products
                .Include(p => p.Variants).Include(p => p.TaxTreatment)
                .FirstOrDefaultAsync(p => p.Id == request.ProductId && !p.IsDeleted, cancellationToken)
                ?? throw new NotFoundException("Product not found.");

            var schema = string.IsNullOrEmpty(product.VariantAttributes)
                ? []
                : JsonSerializer.Deserialize<List<string>>(product.VariantAttributes) ?? [];

            foreach (var variant in request.Variants)
            {
                var unknown = variant.AttributeValues.Keys.Except(schema, StringComparer.OrdinalIgnoreCase).ToList();
                if (unknown.Count > 0 || variant.AttributeValues.Count == 0)
                    throw new FluentValidation.ValidationException(
                        $"Variant attribute(s) [{string.Join(", ", unknown)}] do not match the product's attribute schema [{string.Join(", ", schema)}].");

                if (!string.IsNullOrWhiteSpace(variant.Sku) &&
                    await context.ProductVariants.AnyAsync(v => v.Sku == variant.Sku, cancellationToken))
                    throw new ConflictException($"A variant with SKU '{variant.Sku}' already exists.");

                context.ProductVariants.Add(new Domain.Models.Catalog.ProductVariant
                {
                    TenantId = product.TenantId,
                    ProductId = product.Id,
                    Product = product,
                    AttributeValues = JsonSerializer.Serialize(variant.AttributeValues),
                    Sku = variant.Sku,
                    Barcode = variant.Barcode,
                    SellingPrice = variant.SellingPrice,
                    CostPrice = variant.CostPrice,
                });
            }

            if (product.TrackingMode == TrackingMode.Simple) product.TrackingMode = TrackingMode.Variant;
            await context.SaveChangesAsync(cancellationToken);
            return ProductMapping.ToDto(product);
        }
    }

    public class CreateCategoryCommandHandler(IAppDbContext context) : IRequestHandler<CreateCategoryCommand, CategoryDto>
    {
        public async Task<CategoryDto> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
        {
            if (await context.Categories.AnyAsync(
                    c => c.Name == request.Name && c.ParentId == request.ParentId && !c.IsDeleted, cancellationToken))
                throw new ConflictException($"Category '{request.Name}' already exists at this level.");

            var category = new Category { Name = request.Name, ParentId = request.ParentId };
            context.Categories.Add(category);
            await context.SaveChangesAsync(cancellationToken);
            return new CategoryDto { Id = category.Id, Name = category.Name, ParentId = category.ParentId };
        }
    }

    public class UpdateCategoryCommandHandler(IAppDbContext context) : IRequestHandler<UpdateCategoryCommand, CategoryDto>
    {
        public async Task<CategoryDto> Handle(UpdateCategoryCommand request, CancellationToken cancellationToken)
        {
            var category = await context.Categories
                .FirstOrDefaultAsync(c => c.Id == request.Id && !c.IsDeleted, cancellationToken)
                ?? throw new NotFoundException("Category not found.");

            if (await context.Categories.AnyAsync(
                    c => c.Name == request.Name && c.ParentId == request.ParentId && c.Id != category.Id && !c.IsDeleted,
                    cancellationToken))
                throw new ConflictException($"Category '{request.Name}' already exists at this level.");

            category.Name = request.Name;
            category.ParentId = request.ParentId;
            await context.SaveChangesAsync(cancellationToken);
            return new CategoryDto { Id = category.Id, Name = category.Name, ParentId = category.ParentId };
        }
    }

    public class DeleteCategoryCommandHandler(IAppDbContext context) : IRequestHandler<DeleteCategoryCommand, bool>
    {
        public async Task<bool> Handle(DeleteCategoryCommand request, CancellationToken cancellationToken)
        {
            var category = await context.Categories
                .FirstOrDefaultAsync(c => c.Id == request.Id && !c.IsDeleted, cancellationToken)
                ?? throw new NotFoundException("Category not found.");
            category.IsDeleted = true;
            category.DeletedAt = DateTime.UtcNow;
            category.RecoveryExpiresAt = category.DeletedAt.Value.AddDays(30);
            await context.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
