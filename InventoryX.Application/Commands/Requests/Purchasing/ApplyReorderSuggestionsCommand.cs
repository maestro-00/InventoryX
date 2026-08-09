using InventoryX.Application.Behaviors;
using InventoryX.Application.Services.IServices;
using InventoryX.Domain.Models.Purchasing;
using MediatR;

namespace InventoryX.Application.Commands.Requests.Purchasing;

public sealed record ReorderSuggestionSelection(Guid ProductId, Guid SupplierId, decimal Qty, decimal UnitCost);

public sealed record ApplyReorderSuggestionsCommand(
    IReadOnlyList<ReorderSuggestionSelection> Selections,
    Guid DeliverToLocationId,
    DateTime? RequiredBy = null) : IRequest<IReadOnlyList<PurchaseOrderDto>>, IFeatureGatedCommand
{
    public PlanFeature Feature => PlanFeature.PurchaseOrders;
}
