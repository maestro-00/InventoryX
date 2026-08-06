using InventoryX.Application.Queries.Requests.Reports;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryX.Presentation.Controllers.v1;

[Route("api/v1/reports")]
[Authorize(Roles = "Owner,Administrator,Manager,Accountant")]
public sealed class ReportsController(ISender sender) : ApiControllerBase
{
    [HttpGet("sales")]
    public Task<SalesReportDto> Sales([FromQuery] ReportFilter filter, CancellationToken ct) => sender.Send(new GetSalesReportQuery(filter), ct);

    [HttpGet("profit")]
    [Authorize(Roles = "Owner,Administrator,Manager,Accountant")]
    public Task<ProfitReportDto> Profit([FromQuery] ReportFilter filter, CancellationToken ct) => sender.Send(new GetProfitReportQuery(filter), ct);

    [HttpGet("stock")]
    public Task<StockReportDto> Stock([FromQuery] ReportFilter filter, CancellationToken ct) => sender.Send(new GetStockReportQuery(filter), ct);

    [HttpGet("purchasing")]
    public Task<PurchasingReportDto> Purchasing([FromQuery] ReportFilter filter, CancellationToken ct) => sender.Send(new GetPurchasingReportQuery(filter), ct);

    [HttpGet("staff")]
    public Task<StaffReportDto> Staff([FromQuery] ReportFilter filter, CancellationToken ct) => sender.Send(new GetStaffReportQuery(filter), ct);

    [HttpGet("tax")]
    public Task<TaxReportDto> Tax([FromQuery] ReportFilter filter, CancellationToken ct) =>
        sender.Send(new GetTaxReportQuery(filter.From, filter.To, filter.LocationId, filter.CategoryId, filter.StaffId), ct);
}
