using MediatR;

namespace InventoryX.Application.Queries.Requests.Inventory;

public sealed record GetBatchTraceQuery(Guid BatchId) : IRequest<BatchTraceDto>;
public sealed record BatchTraceSupplierDto(Guid Id, string Name, string? Email, string? Phone);
public sealed record BatchTraceReceiptDto(Guid Id, string ReceiptNumber, DateTime ReceivedAt, decimal Quantity, decimal DamagedQuantity, Guid LocationId);
public sealed record BatchTraceSaleDto(Guid Id, DateTime OccurredAt, decimal Quantity, string CashierId, Guid LocationId);
public sealed record BatchTraceDto(Guid BatchId, string BatchNumber, Guid ProductId, Guid? VariantId, DateTime? ExpiresAt,
    BatchTraceSupplierDto? Supplier, IReadOnlyList<BatchTraceReceiptDto> Receipts, IReadOnlyList<BatchTraceSaleDto> Sales);
