using InventoryX.Application.Behaviors;
using MediatR;

namespace InventoryX.Application.Commands.Requests.Inventory;

public sealed record TransferLineInput(Guid ProductId, decimal Quantity, Guid? VariantId = null, Guid? BatchId = null);
public sealed record ReceiveTransferLineInput(Guid LineId, decimal QuantityReceived);
public sealed record StockTransferResult(Guid Id, string Status, string? DiscrepancyReason = null);

public sealed class CreateStockTransferCommand : IRequest<StockTransferResult>, ITenantWriteCommand, IAuditedCommand
{
    public Guid FromLocationId { get; init; }
    public Guid ToLocationId { get; init; }
    public List<TransferLineInput> Lines { get; init; } = [];
    public string AuditAction => "stock.transfer.create";
    public string AuditEntityType => "StockTransfer";
    public string AuditEntityId => FromLocationId.ToString();
}

public sealed class DispatchStockTransferCommand : IRequest<StockTransferResult>, ITenantWriteCommand, IAuditedCommand
{
    public Guid TransferId { get; init; }
    public string AuditAction => "stock.transfer.dispatch";
    public string AuditEntityType => "StockTransfer";
    public string AuditEntityId => TransferId.ToString();
}

public sealed class ReceiveStockTransferCommand : IRequest<StockTransferResult>, ITenantWriteCommand, IAuditedCommand
{
    public Guid TransferId { get; init; }
    public List<ReceiveTransferLineInput> Lines { get; init; } = [];
    public string? DiscrepancyReason { get; init; }
    public string AuditAction => "stock.transfer.receive";
    public string AuditEntityType => "StockTransfer";
    public string AuditEntityId => TransferId.ToString();
}
