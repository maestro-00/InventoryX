using InventoryX.Application.Behaviors;
using InventoryX.Application.DTOs.Selling;
using MediatR;

namespace InventoryX.Application.Commands.Requests.Selling
{
    public class CreateReturnLineDto
    {
        public Guid SaleLineId { get; init; }
        public decimal Qty { get; init; }
        /// <summary>ToStock | Quarantine (defaults to ToStock).</summary>
        public string Disposition { get; init; } = "ToStock";
    }

    /// <summary>
    /// Return against an original sale (FR-041). Original price + tax applied
    /// automatically. Above the tenant threshold or receiptless → 423 until a
    /// manager attaches authorization. RefundTender Original | Cash in Cycle 1.
    /// </summary>
    public class CreateReturnCommand : IRequest<ReturnTransactionDto>, ITenantWriteCommand, IAuditedCommand
    {
        public Guid OriginalSaleId { get; init; }
        public List<CreateReturnLineDto> Lines { get; init; } = [];
        public string RefundTender { get; init; } = "Original";
        public string? AuthorizedBy { get; init; }
        public string? Reason { get; init; }

        public string AuditAction => "sale.return";
        public string AuditEntityType => "ReturnTransaction";
        public string AuditEntityId => OriginalSaleId.ToString();
    }

    /// <summary>
    /// Exchange = return + new sale in one transaction; settles the difference
    /// only (FR-041). The nested sale lines/payments follow the CreateSale shape.
    /// </summary>
    public class CreateExchangeCommand : IRequest<ReturnTransactionDto>, ITenantWriteCommand, IAuditedCommand
    {
        public Guid OriginalSaleId { get; init; }
        public List<CreateReturnLineDto> Lines { get; init; } = [];
        public string? AuthorizedBy { get; init; }
        public string? Reason { get; init; }

        // New-sale side of the exchange.
        public Guid RegisterId { get; init; }
        public Guid ShiftId { get; init; }
        public List<CreateSaleLineDto> NewLines { get; init; } = [];
        public List<CreateSalePaymentDto> Payments { get; init; } = [];

        public string AuditAction => "sale.exchange";
        public string AuditEntityType => "ReturnTransaction";
        public string AuditEntityId => OriginalSaleId.ToString();
    }
}
