using MediatR;

namespace InventoryX.Application.Queries.Requests.Purchasing;

public sealed record ReorderSuggestionItem(
    Guid ProductId,
    string ProductName,
    string? Sku,
    Guid? SupplierId,
    string? SupplierName,
    decimal CurrentStock,
    decimal ReorderPoint,
    decimal SuggestedQty,
    int? LeadTimeDays,
    decimal UnitCost);

public sealed record ReorderSuggestionsDto(IReadOnlyList<ReorderSuggestionItem> Items);

public sealed record GetReorderSuggestionsQuery(Guid? LocationId = null) : IRequest<ReorderSuggestionsDto>;
