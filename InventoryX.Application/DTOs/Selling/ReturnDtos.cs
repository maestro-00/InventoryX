namespace InventoryX.Application.DTOs.Selling
{
    public class ReturnLineDto
    {
        public Guid Id { get; init; }
        public Guid SaleLineId { get; init; }
        public Guid ProductId { get; init; }
        public Guid? VariantId { get; init; }
        public decimal Qty { get; init; }
        public decimal OriginalUnitPrice { get; init; }
        public decimal OriginalTaxAmount { get; init; }
        public decimal LineRefund { get; init; }
        public string Disposition { get; init; } = "ToStock";
    }

    public class ReturnTransactionDto
    {
        public Guid Id { get; init; }
        public Guid OriginalSaleId { get; init; }
        public Guid? ExchangeSaleId { get; init; }
        public string Status { get; init; } = "Completed";
        public bool AuthorizationRequired { get; init; }
        public string? AuthorizedBy { get; init; }
        public string RefundTender { get; init; } = "Original";
        public decimal RefundTotal { get; init; }
        public DateTime OccurredAt { get; init; }
        public string? Reason { get; init; }
        public List<ReturnLineDto> Lines { get; init; } = [];
    }
}
