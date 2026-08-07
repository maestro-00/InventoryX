using InventoryX.Application.Commands.Requests.Reports;
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

    [HttpPost("schedules")]
    public Task<ReportScheduleDto> CreateSchedule(CreateReportScheduleCommand command, CancellationToken ct) => sender.Send(command, ct);

    [HttpGet("schedules")]
    public Task<IReadOnlyList<ReportScheduleDto>> ListSchedules(CancellationToken ct) =>
        sender.Send(new GetReportSchedulesQuery(), ct);

    [HttpGet("schedules/{id:guid}")]
    public async Task<ActionResult<ReportScheduleDto>> GetSchedule(Guid id, CancellationToken ct) =>
        (await sender.Send(new GetReportSchedulesQuery(id), ct)).Single();

    [HttpDelete("schedules/{id:guid}")]
    public Task<bool> DeleteSchedule(Guid id, CancellationToken ct) => sender.Send(new DeleteReportScheduleCommand(id), ct);

    [HttpGet("{reportType}/export")]
    public async Task<IActionResult> Export(string reportType, string format, [FromQuery] ReportFilter filter, CancellationToken ct)
    {
        var result = await sender.Send(new ExportReportCommand(reportType, format, filter), ct);
        if (result.Accepted) return AcceptedAtAction(nameof(PollExport), new { id = result.JobId }, new { jobId = result.JobId, status = result.Status.ToString() });
        return File(result.Content!, result.ContentType!, result.FileName);
    }

    [HttpGet("export-jobs/{id:guid}")]
    public async Task<IActionResult> PollExport(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new GetReportExportJobQuery(id), ct);
        if (result.Status == Domain.Models.Auditing.ReportExportStatus.Pending) return Accepted(new { jobId = id, status = "Pending" });
        if (result.Status == Domain.Models.Auditing.ReportExportStatus.Failed) return Problem(result.Error, statusCode: 500);
        return File(result.Content!, result.ContentType!, result.FileName);
    }
}
