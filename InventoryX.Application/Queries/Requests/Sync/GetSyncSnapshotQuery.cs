using MediatR;

namespace InventoryX.Application.Queries.Requests.Sync;

public record SyncProductDto(
    Guid Id,
    string Name,
    string? Sku,
    string? Barcode,
    decimal SellingPrice,
    Guid? TaxTreatmentId,
    bool AllowFractional,
    string TrackingMode,
    bool AllowsDiscount,
    string Version);

public record SyncVariantDto(Guid Id, Guid ProductId, string? Sku, string? Barcode, decimal? SellingPrice, string Version);
public record SyncTaxDto(Guid Id, string Code, string Name, string ComponentsJson, string Version);
public record SyncStockDto(Guid ProductId, Guid? VariantId, Guid? BatchId, decimal QtyOnHand, decimal QtyInTransit, decimal QtyQuarantine, string Version);
public record SyncFavouriteDto(Guid RegisterId, string LayoutJson, string Version);
public record SyncReceiptTemplateDto(string TemplateJson, string Version);
public record SyncDeletedRefDto(string EntityType, Guid Id, string Version);

public record SyncSnapshotDto(
    string Watermark,
    Guid RegisterId,
    Guid LocationId,
    List<SyncProductDto> Products,
    List<SyncVariantDto> Variants,
    List<SyncTaxDto> TaxTreatments,
    List<SyncStockDto> Stock,
    SyncFavouriteDto? Favourites,
    SyncReceiptTemplateDto? ReceiptTemplate,
    List<SyncDeletedRefDto> Deleted,
    string BundleVersion);

public record GetSyncSnapshotQuery(Guid RegisterId, string? Watermark = null) : IRequest<SyncSnapshotDto>;
