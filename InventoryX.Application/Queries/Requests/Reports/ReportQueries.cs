using MediatR;

namespace InventoryX.Application.Queries.Requests.Reports;

public sealed record GetDashboardQuery(DateTime? AsOf = null, bool IncludeProfit = false) : IRequest<DashboardDto>;
public sealed record DashboardMetricDto(decimal Today, decimal SameDayLastWeek, string DetailUrl);
public sealed record DashboardCountMetricDto(int Today, int SameDayLastWeek, string DetailUrl);
public sealed record DashboardTopSellerDto(Guid ProductId, string ProductName, decimal Quantity, decimal Sales, string DetailUrl);
public sealed record DashboardDto(DashboardMetricDto Sales, DashboardCountMetricDto TransactionCount,
    DashboardMetricDto AverageBasket, DashboardMetricDto ItemsSold, DashboardMetricDto CashInDrawer,
    int LowStockWarnings, int ExpiryWarnings, IReadOnlyList<DashboardTopSellerDto> TopSellers, decimal? GrossProfit);

public sealed record ReportFilter(DateTime From, DateTime To, Guid? LocationId = null, Guid? CategoryId = null, string? StaffId = null);
public sealed record GetSalesReportQuery(ReportFilter Filter) : IRequest<SalesReportDto>;
public sealed record GetProfitReportQuery(ReportFilter Filter) : IRequest<ProfitReportDto>;
public sealed record GetStockReportQuery(ReportFilter Filter) : IRequest<StockReportDto>;
public sealed record GetPurchasingReportQuery(ReportFilter Filter) : IRequest<PurchasingReportDto>;
public sealed record GetStaffReportQuery(ReportFilter Filter) : IRequest<StaffReportDto>;
public sealed record GetTaxReportQuery(DateTime From, DateTime To, Guid? LocationId = null, Guid? CategoryId = null, string? StaffId = null) : IRequest<TaxReportDto>
{ public ReportFilter Filter => new(From, To, LocationId, CategoryId, StaffId); }

public sealed record SalesReportRowDto(Guid SaleId, DateTime OccurredAt, Guid LocationId, string StaffId,
    decimal Subtotal, decimal Discount, decimal Tax, decimal Total, string Status);
public sealed record SalesReportDto(ReportFilter Filter, IReadOnlyList<SalesReportRowDto> Rows, decimal TotalSales, int Transactions);
public sealed record ProfitReportRowDto(Guid ProductId, string ProductName, decimal Revenue, decimal Cost, decimal GrossProfit);
public sealed record ProfitReportDto(ReportFilter Filter, IReadOnlyList<ProfitReportRowDto> Rows, decimal GrossProfit);
public sealed record StockReportRowDto(Guid ProductId, string ProductName, Guid LocationId, decimal OnHand, decimal UnitCost, decimal Value);
public sealed record StockReportDto(ReportFilter Filter, IReadOnlyList<StockReportRowDto> Rows, decimal TotalValue);
public sealed record PurchasingReportRowDto(Guid PurchaseOrderId, Guid SupplierId, string SupplierName, string Status,
    DateTime? RequiredBy, decimal OrderedValue, decimal OutstandingQuantity);
public sealed record PurchasingReportDto(ReportFilter Filter, IReadOnlyList<PurchasingReportRowDto> Rows);
public sealed record StaffReportRowDto(string StaffId, int Transactions, decimal Sales, decimal Discounts, int Voids);
public sealed record StaffReportDto(ReportFilter Filter, IReadOnlyList<StaffReportRowDto> Rows);
public sealed record TaxReportComponentDto(string Code, string Name, decimal Rate, decimal Amount);
public sealed record TaxReportDto(DateTime From, DateTime To, IReadOnlyList<TaxReportComponentDto> Components, decimal TotalTax);
