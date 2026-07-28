using InventoryX.Application.Behaviors;
using InventoryX.Application.DTOs.Selling;
using MediatR;

namespace InventoryX.Application.Commands.Requests.Selling
{
    /// <summary>
    /// Voids a Completed sale (FR-041). Permission-gated (VoidSale) and
    /// audit-logged; reverses the stock effect so the ledger stays truthful.
    /// </summary>
    public class VoidSaleCommand : IRequest<SaleDto>, ITenantWriteCommand, IAuditedCommand
    {
        public Guid SaleId { get; init; }
        public string? Reason { get; init; }

        public string AuditAction => "sale.void";
        public string AuditEntityType => "Sale";
        public string AuditEntityId => SaleId.ToString();
    }
}
