using InventoryX.Application.Commands.Requests.Purchasing;
using InventoryX.Application.Exceptions;
using InventoryX.Application.Repository;
using InventoryX.Domain.Models.Purchasing;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InventoryX.Application.Commands.RequestHandlers.Purchasing;

public sealed class ApplyReorderSuggestionsCommandHandler(IAppDbContext context)
    : IRequestHandler<ApplyReorderSuggestionsCommand, IReadOnlyList<PurchaseOrderDto>>
{
    public async Task<IReadOnlyList<PurchaseOrderDto>> Handle(
        ApplyReorderSuggestionsCommand request, CancellationToken cancellationToken)
    {
        if (request.Selections.Count == 0)
            throw new FluentValidation.ValidationException("At least one reorder selection is required.");

        if (!await context.Locations.AnyAsync(l => l.Id == request.DeliverToLocationId && !l.IsDeleted, cancellationToken))
            throw new NotFoundException("Delivery location not found.");

        // Group selections by supplier to create one PO per supplier
        var bySupplier = request.Selections
            .GroupBy(s => s.SupplierId)
            .ToList();

        var createdOrders = new List<PurchaseOrderDto>();

        foreach (var group in bySupplier)
        {
            var supplierId = group.Key;
            if (!await context.Suppliers.AnyAsync(s => s.Id == supplierId, cancellationToken))
                throw new NotFoundException($"Supplier {supplierId} not found.");

            var products = await context.Products
                .Where(p => group.Select(g => g.ProductId).Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, cancellationToken);

            var lines = group.Select(sel =>
            {
                var product = products.TryGetValue(sel.ProductId, out var p) ? p : null;
                return new PurchaseOrderLine
                {
                    ProductId = sel.ProductId,
                    Description = product?.Name ?? sel.ProductId.ToString(),
                    OrderedQty = sel.Qty,
                    UnitCost = sel.UnitCost,
                };
            }).ToList();

            var order = new PurchaseOrder
            {
                SupplierId = supplierId,
                DeliverToLocationId = request.DeliverToLocationId,
                Origin = PurchaseOrderOrigin.ReorderSuggestion,
                RequiredBy = request.RequiredBy,
                Lines = lines,
            };

            context.PurchaseOrders.Add(order);
            await context.SaveChangesAsync(cancellationToken);
            createdOrders.Add(PurchaseOrderCommandHandler.Map(order));
        }

        return createdOrders;
    }
}
