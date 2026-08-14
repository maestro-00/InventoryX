using InventoryX.Application.Behaviors;
using MediatR;

namespace InventoryX.Application.Commands.Requests.Sync;

public record RejectedOfflineSaleDto(
    Guid Id,
    Guid ClientSaleId,
    Guid RegisterId,
    string RejectionReason,
    string? TraceId,
    string Status,
    Guid? LinkedReconciliationSaleId,
    string PayloadHash);

public record ResolveRejectedOfflineSaleResult(
    Guid Id,
    Guid ClientSaleId,
    string Status,
    Guid? LinkedReconciliationSaleId);

public sealed class ResolveRejectedOfflineSaleCommand : IRequest<ResolveRejectedOfflineSaleResult>, IAuditedCommand
{
    public Guid RejectedSaleId { get; init; }
    /// <summary>retryRelease | reconcileLinked</summary>
    public string Resolution { get; init; } = string.Empty;
    public Guid? LinkedReconciliationSaleId { get; init; }
    public string? Note { get; init; }

    public string AuditAction => "offline_sale.resolve_rejected";
    public string AuditEntityType => "RejectedOfflineSale";
    public string AuditEntityId => RejectedSaleId.ToString();
}

public sealed class ListRejectedOfflineSalesQuery : IRequest<List<RejectedOfflineSaleDto>>;
