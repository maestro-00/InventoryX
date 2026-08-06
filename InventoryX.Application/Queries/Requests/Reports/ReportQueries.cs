using MediatR;

namespace InventoryX.Application.Queries.Requests.Reports;

public sealed record GetDashboardQuery(DateTime? AsOf = null, bool IncludeProfit = false) : IRequest<DashboardDto>;
public sealed record DashboardMetricDto(decimal Today, decimal SameDayLastWeek, string DetailUrl);
public sealed record DashboardCountMetricDto(int Today, int SameDayLastWeek, string DetailUrl);
public sealed record DashboardTopSellerDto(Guid ProductId, string ProductName, decimal Quantity, decimal Sales, string DetailUrl);
public sealed record DashboardDto(DashboardMetricDto Sales, DashboardCountMetricDto TransactionCount,
    DashboardMetricDto AverageBasket, DashboardMetricDto ItemsSold, DashboardMetricDto CashInDrawer,
    int LowStockWarnings, int ExpiryWarnings, IReadOnlyList<DashboardTopSellerDto> TopSellers, decimal? GrossProfit);

public sealed record GetTaxReportQuery(DateTime From, DateTime To, Guid? LocationId = null) : IRequest<TaxReportDto>;
public sealed record TaxReportComponentDto(string Code, string Name, decimal Rate, decimal Amount);
public sealed record TaxReportDto(DateTime From, DateTime To, IReadOnlyList<TaxReportComponentDto> Components, decimal TotalTax);
