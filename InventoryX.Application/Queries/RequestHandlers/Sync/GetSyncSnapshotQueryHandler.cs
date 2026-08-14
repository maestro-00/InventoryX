using InventoryX.Application.Exceptions;
using InventoryX.Application.Queries.Requests.Sync;
using InventoryX.Application.Repository;
using InventoryX.Application.Services.IServices;
using InventoryX.Domain.Models.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InventoryX.Application.Queries.RequestHandlers.Sync;

public sealed class GetSyncSnapshotQueryHandler(
    IAppDbContext context,
    ITenantContext tenantContext) : IRequestHandler<GetSyncSnapshotQuery, SyncSnapshotDto>
{
    public const string BundleVersion = "2026.08.offline-prep.1";

    public async Task<SyncSnapshotDto> Handle(GetSyncSnapshotQuery request, CancellationToken cancellationToken)
    {
        var register = await context.Registers.AsNoTracking().SingleOrDefaultAsync(r => r.Id == request.RegisterId && r.IsActive, cancellationToken)
            ?? throw new NotFoundException("Register not found.");
        var after = Decode(request.Watermark);

        var productsAll = await context.Products.AsNoTracking().ToListAsync(cancellationToken);
        var variantsAll = await context.ProductVariants.AsNoTracking().ToListAsync(cancellationToken);
        var taxesAll = await context.TaxTreatments.AsNoTracking().ToListAsync(cancellationToken);
        var stockAll = await context.StockLevels.AsNoTracking().Where(s => s.LocationId == register.LocationId).ToListAsync(cancellationToken);
        var favourites = await context.FavouritesLayouts.AsNoTracking()
            .SingleOrDefaultAsync(f => f.RegisterId == register.Id, cancellationToken);
        var tenant = tenantContext.TenantId is Guid tenantId
            ? await context.Tenants.AsNoTracking().SingleOrDefaultAsync(t => t.Id == tenantId, cancellationToken)
            : null;

        var activeProducts = productsAll.Where(p => !p.IsDeleted).ToList();
        var products = activeProducts.Where(p => Version(p) > after).ToList();
        var variants = variantsAll.Where(v => Version(v) > after).ToList();
        var taxes = taxesAll.Where(t => Version(t) > after).ToList();
        var stock = stockAll.Where(s => Version(s) > after).ToList();

        var deleted = productsAll
            .Where(p => p.IsDeleted && Version(p) > after)
            .Select(p => new SyncDeletedRefDto("product", p.Id, Encode(Version(p))))
            .ToList();

        var currentCandidates = productsAll.Cast<GlobalModel>()
            .Concat(variantsAll)
            .Concat(taxesAll)
            .Concat(stockAll);
        if (favourites is not null) currentCandidates = currentCandidates.Append(favourites);
        var current = currentCandidates.Select(Version).DefaultIfEmpty(after).Max();

        SyncFavouriteDto? favouriteDto = null;
        if (favourites is not null && (request.Watermark is null || Version(favourites) > after))
        {
            favouriteDto = new SyncFavouriteDto(favourites.RegisterId, favourites.LayoutJson, Encode(Version(favourites)));
        }

        SyncReceiptTemplateDto? receiptDto = null;
        if (tenant?.ReceiptTemplate is not null &&
            (request.Watermark is null || Version(tenant) > after))
        {
            receiptDto = new SyncReceiptTemplateDto(tenant.ReceiptTemplate, Encode(Version(tenant)));
        }

        return new SyncSnapshotDto(
            Encode(current),
            register.Id,
            register.LocationId,
            products.Select(p => new SyncProductDto(
                p.Id, p.Name, p.Sku, p.Barcode, p.SellingPrice, p.TaxTreatmentId,
                p.AllowFractional, p.TrackingMode.ToString(), AllowsDiscount: true, Encode(Version(p)))).ToList(),
            variants.Select(v => new SyncVariantDto(v.Id, v.ProductId, v.Sku, v.Barcode, v.SellingPrice, Encode(Version(v)))).ToList(),
            taxes.Select(t => new SyncTaxDto(t.Id, t.Code, t.Name, t.ComponentsJson, Encode(Version(t)))).ToList(),
            stock.Select(s => new SyncStockDto(s.ProductId, s.VariantId, s.BatchId, s.QtyOnHand, s.QtyInTransit, s.QtyQuarantine, Encode(Version(s)))).ToList(),
            favouriteDto,
            receiptDto,
            deleted,
            BundleVersion);
    }

    private static long Version(GlobalModel entity)
    {
        if (entity.RowVersion is { Length: > 0 })
        {
            var bytes = entity.RowVersion.Length >= 8 ? entity.RowVersion[^8..] : [.. new byte[8 - entity.RowVersion.Length], .. entity.RowVersion];
            if (BitConverter.IsLittleEndian) Array.Reverse(bytes);
            return unchecked((long)BitConverter.ToUInt64(bytes));
        }
        return (entity.UpdatedAt ?? entity.CreatedAt).Ticks;
    }

    private static string Encode(long value) => Convert.ToBase64String(BitConverter.GetBytes(value));
    private static long Decode(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return 0;
        try
        {
            var bytes = Convert.FromBase64String(value);
            return bytes.Length == 8 ? BitConverter.ToInt64(bytes) : throw new FormatException();
        }
        catch (FormatException) { throw new FluentValidation.ValidationException("Invalid sync watermark."); }
    }
}
