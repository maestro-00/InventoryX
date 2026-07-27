using InventoryX.Application.Commands.Requests.Tenancy;
using InventoryX.Application.Exceptions;
using InventoryX.Application.Queries.Requests.Tenancy;
using InventoryX.Application.Repository;
using InventoryX.Application.Services.IServices;
using InventoryX.Domain.Models.Catalog;
using InventoryX.Domain.Models.Tenancy;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InventoryX.Application.Commands.RequestHandlers.Tenancy
{
    public static class TenantMapping
    {
        public static TenantDto ToDto(Tenant tenant) => new()
        {
            Id = tenant.Id,
            Name = tenant.Name,
            Country = tenant.Country,
            Currency = tenant.Currency,
            BusinessType = tenant.BusinessType.ToString(),
            ValuationMethod = tenant.ValuationMethod.ToString(),
            OnboardingChecklist = tenant.OnboardingChecklist,
            SampleDataLoaded = tenant.SampleDataLoaded,
            AdjustmentApprovalThreshold = tenant.AdjustmentApprovalThreshold,
            PoApprovalThreshold = tenant.PoApprovalThreshold,
            TillVarianceThreshold = tenant.TillVarianceThreshold,
            ReturnAuthorizationThreshold = tenant.ReturnAuthorizationThreshold,
            RequireExpiryOnBatchReceipt = tenant.RequireExpiryOnBatchReceipt,
            BillingEmail = tenant.BillingEmail,
            Address = tenant.Address,
            Phone = tenant.Phone,
        };

        public static async Task<Tenant> CurrentAsync(IAppDbContext context, ITenantContext tenantContext, CancellationToken ct) =>
            await context.Tenants.FirstOrDefaultAsync(t => t.Id == tenantContext.TenantId, ct)
                ?? throw new NotFoundException("Tenant not found.");
    }

    public class GetTenantQueryHandler(IAppDbContext context, ITenantContext tenantContext)
        : IRequestHandler<GetTenantQuery, TenantDto>
    {
        public async Task<TenantDto> Handle(GetTenantQuery request, CancellationToken cancellationToken) =>
            TenantMapping.ToDto(await TenantMapping.CurrentAsync(context, tenantContext, cancellationToken));
    }

    public class UpdateTenantCommandHandler(IAppDbContext context, ITenantContext tenantContext)
        : IRequestHandler<UpdateTenantCommand, TenantDto>
    {
        public async Task<TenantDto> Handle(UpdateTenantCommand request, CancellationToken cancellationToken)
        {
            var tenant = await TenantMapping.CurrentAsync(context, tenantContext, cancellationToken);

            tenant.Name = request.Name ?? tenant.Name;
            tenant.Address = request.Address ?? tenant.Address;
            tenant.Phone = request.Phone ?? tenant.Phone;
            tenant.BillingEmail = request.BillingEmail ?? tenant.BillingEmail;
            tenant.AdjustmentApprovalThreshold = request.AdjustmentApprovalThreshold ?? tenant.AdjustmentApprovalThreshold;
            tenant.PoApprovalThreshold = request.PoApprovalThreshold ?? tenant.PoApprovalThreshold;
            tenant.TillVarianceThreshold = request.TillVarianceThreshold ?? tenant.TillVarianceThreshold;
            tenant.ReturnAuthorizationThreshold = request.ReturnAuthorizationThreshold ?? tenant.ReturnAuthorizationThreshold;
            tenant.RequireExpiryOnBatchReceipt = request.RequireExpiryOnBatchReceipt ?? tenant.RequireExpiryOnBatchReceipt;
            tenant.OnboardingChecklist = request.OnboardingChecklist ?? tenant.OnboardingChecklist;

            if (request.ValuationMethod is not null)
            {
                if (!Enum.TryParse<ValuationMethod>(request.ValuationMethod, true, out var method))
                    throw new FluentValidation.ValidationException("Unknown valuation method.");
                if (method != tenant.ValuationMethod && !request.ConfirmValuationChange)
                    throw new FluentValidation.ValidationException(
                        "Changing the valuation method requires confirmValuationChange=true (FR-028).");
                if (method is not ValuationMethod.WeightedAverage)
                    throw new FluentValidation.ValidationException(
                        "Only WeightedAverage valuation is available in this cycle.");
                tenant.ValuationMethod = method;
            }

            await context.SaveChangesAsync(cancellationToken);
            return TenantMapping.ToDto(tenant);
        }
    }

    public class LoadSampleDataCommandHandler(IAppDbContext context, ITenantContext tenantContext)
        : IRequestHandler<LoadSampleDataCommand, bool>
    {
        public async Task<bool> Handle(LoadSampleDataCommand request, CancellationToken cancellationToken)
        {
            var tenant = await TenantMapping.CurrentAsync(context, tenantContext, cancellationToken);
            if (tenant.SampleDataLoaded) throw new ConflictException("Sample data is already loaded.");

            var tax = await context.TaxTreatments.FirstOrDefaultAsync(t => t.Code == "GH-STD", cancellationToken);
            var category = new Category { Name = "Sample - Groceries" };
            context.Categories.Add(category);
            context.Products.AddRange(
                new Product { Name = "Sample Sugar 1kg", Sku = "SAMPLE-001", SellingPrice = 10m, CostPrice = 6m, Category = category, TaxTreatmentId = tax?.Id, IsSampleData = true },
                new Product { Name = "Sample Rice 5kg", Sku = "SAMPLE-002", SellingPrice = 85m, CostPrice = 60m, Category = category, TaxTreatmentId = tax?.Id, IsSampleData = true },
                new Product { Name = "Sample Cooking Oil 1L", Sku = "SAMPLE-003", SellingPrice = 30m, CostPrice = 22m, Category = category, TaxTreatmentId = tax?.Id, IsSampleData = true });

            tenant.SampleDataLoaded = true;
            await context.SaveChangesAsync(cancellationToken);
            return true;
        }
    }

    public class RemoveSampleDataCommandHandler(IAppDbContext context, ITenantContext tenantContext)
        : IRequestHandler<RemoveSampleDataCommand, bool>
    {
        public async Task<bool> Handle(RemoveSampleDataCommand request, CancellationToken cancellationToken)
        {
            var tenant = await TenantMapping.CurrentAsync(context, tenantContext, cancellationToken);

            var sampleProducts = await context.Products.Where(p => p.IsSampleData).ToListAsync(cancellationToken);
            var sampleIds = sampleProducts.Select(p => p.Id).ToList();
            context.StockLevels.RemoveRange(context.StockLevels.Where(s => sampleIds.Contains(s.ProductId)));
            context.StockMovements.RemoveRange(context.StockMovements.Where(m => sampleIds.Contains(m.ProductId)));
            context.Products.RemoveRange(sampleProducts);
            var sampleCategories = await context.Categories
                .Where(c => c.Name.StartsWith("Sample - "))
                .ToListAsync(cancellationToken);
            context.Categories.RemoveRange(sampleCategories);

            tenant.SampleDataLoaded = false;
            await context.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
