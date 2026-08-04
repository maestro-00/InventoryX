using MediatR;

namespace InventoryX.Application.Queries.Requests.Purchasing;

public sealed record SupplierPerformanceDto(
    Guid SupplierId,
    string SupplierName,
    int TotalOrders,
    int OnTimeOrders,
    double OnTimeRatePercent,
    double? AverageLeadTimeDays,
    IReadOnlyList<SupplierPriceHistoryItem> PriceHistory);

public sealed record SupplierPriceHistoryItem(
    Guid ProductId,
    string ProductName,
    decimal UnitCost,
    DateTime ReceivedAt);

public sealed record GetSupplierPerformanceQuery(Guid SupplierId) : IRequest<SupplierPerformanceDto>;
