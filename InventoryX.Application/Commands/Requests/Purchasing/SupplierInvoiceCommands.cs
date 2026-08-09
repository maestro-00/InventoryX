using InventoryX.Application.Behaviors;
using MediatR;

namespace InventoryX.Application.Commands.Requests.Purchasing;

public sealed record SupplierInvoiceLineInput(Guid ProductId, Guid? VariantId, decimal Qty, decimal UnitPrice);

public sealed record SupplierInvoiceLineDto(
    Guid Id, Guid ProductId, Guid? VariantId, decimal Qty, decimal UnitPrice,
    decimal? OrderedUnitCost, bool HasVariance);

public sealed record SupplierInvoiceDto(
    Guid Id, Guid SupplierId, Guid? PurchaseOrderId, string InvoiceNumber,
    DateTime InvoiceDate, bool HasPriceVariance, string? Notes,
    IReadOnlyList<SupplierInvoiceLineDto> Lines);

public sealed class RecordSupplierInvoiceCommand : IRequest<SupplierInvoiceDto>, ITenantWriteCommand, IAuditedCommand
{
    public Guid SupplierId { get; init; }
    public Guid? PurchaseOrderId { get; init; }
    public required string InvoiceNumber { get; init; }
    public DateTime InvoiceDate { get; init; }
    public string? Notes { get; init; }
    public List<SupplierInvoiceLineInput> Lines { get; init; } = [];
    public string AuditAction => "supplier_invoice.record";
    public string AuditEntityType => "SupplierInvoice";
    public string AuditEntityId => SupplierId.ToString();
}

public sealed record LandedCostLineInput(Guid GoodsReceiptLineId, decimal Amount);

public sealed record LandedCostAllocationDto(
    Guid GoodsReceiptId, IReadOnlyList<LandedCostLineAllocationDto> Lines);

public sealed record LandedCostLineAllocationDto(
    Guid GoodsReceiptLineId, Guid ProductId, decimal AllocatedAmount, decimal NewUnitCost);

public sealed class AllocateLandedCostsCommand : IRequest<LandedCostAllocationDto>, ITenantWriteCommand, IAuditedCommand
{
    public Guid GoodsReceiptId { get; set; }
    public string CostType { get; init; } = "Freight"; // Freight|Duty|Clearing|Insurance
    public decimal TotalAmount { get; init; }
    public string? Notes { get; init; }
    public string AuditAction => "landed_cost.allocate";
    public string AuditEntityType => "GoodsReceipt";
    public string AuditEntityId => GoodsReceiptId.ToString();
}
