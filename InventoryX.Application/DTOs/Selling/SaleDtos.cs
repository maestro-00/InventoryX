namespace InventoryX.Application.DTOs.Selling
{
    public class SaleLineDto
    {
        public Guid Id { get; init; }
        public Guid ProductId { get; init; }
        public Guid? VariantId { get; init; }
        public Guid? BatchId { get; init; }
        public string ProductName { get; init; } = string.Empty;
        public decimal Qty { get; init; }
        public decimal UnitPrice { get; init; }
        public decimal LineDiscount { get; init; }
        public decimal TaxAmount { get; init; }
        public decimal LineTotal { get; init; }
        public string TaxComponents { get; init; } = "[]";
        public string? Note { get; init; }
    }

    public class SalePaymentDto
    {
        public string Tender { get; init; } = "Cash";
        public decimal Amount { get; init; }
        public string? Reference { get; init; }
    }

    public class SaleDto
    {
        public Guid Id { get; init; }
        public Guid ClientSaleId { get; init; }
        public Guid LocationId { get; init; }
        public Guid RegisterId { get; init; }
        public Guid ShiftId { get; init; }
        public string CashierId { get; init; } = string.Empty;
        public string Status { get; init; } = "Completed";
        public decimal Subtotal { get; init; }
        public decimal DiscountTotal { get; init; }
        public decimal TaxTotal { get; init; }
        public decimal GrandTotal { get; init; }
        public decimal ChangeDue { get; init; }
        public bool StockConflictFlag { get; init; }
        public DateTime OccurredAt { get; init; }
        public List<SaleLineDto> Lines { get; init; } = [];
        public List<SalePaymentDto> Payments { get; init; } = [];
    }

}
