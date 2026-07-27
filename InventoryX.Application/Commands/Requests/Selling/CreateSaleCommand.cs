using InventoryX.Application.Behaviors;
using InventoryX.Application.DTOs.Selling;
using InventoryX.Domain.Models.Tenancy;
using MediatR;

namespace InventoryX.Application.Commands.Requests.Selling
{
    public class CreateSaleLineDto
    {
        public Guid ProductId { get; init; }
        public Guid? VariantId { get; init; }
        public Guid? BatchId { get; init; }
        public decimal Qty { get; init; }
        /// <summary>Explicit price override; server snapshots the catalogue price otherwise.</summary>
        public decimal? UnitPrice { get; init; }
        public decimal LineDiscount { get; init; }
        public string? DiscountAuthorizedBy { get; init; }
        public string? Note { get; init; }
    }

    public class CreateSalePaymentDto
    {
        public string Tender { get; init; } = "Cash";
        public decimal Amount { get; init; }
        public string? Reference { get; init; }
    }

    /// <summary>
    /// Creates a Completed (or Held, US2) sale with price + Ghana tax snapshot
    /// per line, ledger decrement and usage-counter increment (T042).
    /// Idempotent by ClientSaleId (research R6).
    /// </summary>
    public class CreateSaleCommand : IRequest<SaleDto>, IPlanLimitedCommand
    {
        public Guid ClientSaleId { get; init; } = Guid.NewGuid();
        public Guid RegisterId { get; init; }
        public Guid ShiftId { get; init; }
        public string Status { get; init; } = "Completed";
        public List<CreateSaleLineDto> Lines { get; init; } = [];
        public List<CreateSalePaymentDto> Payments { get; init; } = [];
        public DateTime? OccurredAt { get; init; }
        public bool OfflineOrigin { get; init; }
        /// <summary>Offline ingest may drive stock negative; sets the conflict flag instead of failing.</summary>
        public bool AllowNegativeStock { get; init; }

        public UsageMetric Metric => UsageMetric.SalesThisMonth;
    }
}
