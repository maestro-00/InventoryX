using InventoryX.Application.Commands.Requests.Selling;
using InventoryX.Application.DTOs.Selling;
using InventoryX.Application.Exceptions;
using InventoryX.Application.Repository;
using InventoryX.Application.Services.IServices;
using InventoryX.Domain.Models.Inventory;
using InventoryX.Domain.Models.Selling;
using InventoryX.Domain.Models.Tenancy;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InventoryX.Application.Commands.RequestHandlers.Selling
{
    public class VoidSaleCommandHandler(
        IAppDbContext context,
        IStockLedger stockLedger,
        ITenantContext tenantContext) : IRequestHandler<VoidSaleCommand, SaleDto>
    {
        public async Task<SaleDto> Handle(VoidSaleCommand request, CancellationToken cancellationToken)
        {
            // Permission gate: only roles carrying the VoidSale atom may void.
            if (string.IsNullOrWhiteSpace(tenantContext.Role))
                throw new ConflictException("A role is required to void a sale.");
            var role = await context.AppRoles
                .FirstOrDefaultAsync(r => r.Name == tenantContext.Role, cancellationToken);
            if (role is null || (role.Permissions & Permission.VoidSale) != Permission.VoidSale)
                throw new ConflictException("Your role is not permitted to void sales.");

            var sale = await context.Sales
                .Include(s => s.Lines).Include(s => s.Payments)
                .FirstOrDefaultAsync(s => s.Id == request.SaleId, cancellationToken)
                ?? throw new NotFoundException("Sale not found.");
            if (sale.Status == SaleStatus.Voided)
                throw new ConflictException("Sale is already voided.");
            if (sale.Status == SaleStatus.Held)
                throw new ConflictException("Held sales have no stock effect; recall or discard them instead of voiding.");

            // Reverse the stock effect of every sold line so the ledger stays truthful.
            var movements = sale.Lines
                .Select(line => new StockMovementRequest(
                    MovementType.ReturnIn,
                    line.ProductId,
                    sale.LocationId,
                    line.Qty - line.QtyReturned,
                    VariantId: line.VariantId,
                    BatchId: line.BatchId,
                    ReasonCode: "Void",
                    CorrelationId: sale.Id,
                    OccurredAt: DateTime.UtcNow))
                .Where(m => m.QtyDelta != 0)
                .ToList();
            if (movements.Count > 0)
                await stockLedger.AppendAsync(movements, cancellationToken);

            sale.Status = SaleStatus.Voided;
            sale.VoidedBy = tenantContext.UserId;
            sale.VoidedAt = DateTime.UtcNow;
            await context.SaveChangesAsync(cancellationToken);
            return SaleMapping.ToDto(sale);
        }
    }
}
