using InventoryX.Application.Commands.Requests.Tenancy;
using InventoryX.Application.Commands.Requests.Selling;
using InventoryX.Application.DTOs.Selling;
using InventoryX.Application.Queries.Requests.Selling;
using InventoryX.Application.Queries.Requests.Tenancy;
using MediatR;
using InventoryX.Application.Services.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using InventoryX.Application.Repository;
using InventoryX.Domain.Models.Auditing;
using Microsoft.EntityFrameworkCore;

namespace InventoryX.Presentation.Controllers.v1;

[Route("api/v1/tenant")]
[Authorize]
public sealed class TenantController(ISender sender, ITenantExportService exportService, IAppDbContext context) : ApiControllerBase
{
    public sealed record ReceiptTemplateRequest(string TemplateJson);

    [HttpGet]
    public async Task<ActionResult<TenantDto>> Get(CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetTenantQuery(), cancellationToken));

    [HttpPatch]
    [Authorize(Roles = "Owner,Administrator")]
    public async Task<ActionResult<TenantDto>> Update(
        UpdateTenantCommand command,
        CancellationToken cancellationToken) =>
        Ok(await sender.Send(command, cancellationToken));

    [HttpGet("receipt-template")]
    public async Task<ActionResult<ReceiptTemplateDto>> GetReceiptTemplate(CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetReceiptTemplateQuery(), cancellationToken));

    [HttpPut("receipt-template")]
    [Authorize(Roles = "Owner,Administrator")]
    public async Task<ActionResult<ReceiptTemplateDto>> PutReceiptTemplate(
        ReceiptTemplateRequest request,
        CancellationToken cancellationToken) =>
        Ok(await sender.Send(new UpdateReceiptTemplateCommand(request.TemplateJson), cancellationToken));

    [HttpPost("sample-data")]
    [Authorize(Roles = "Owner,Administrator")]
    public async Task<IActionResult> LoadSampleData(CancellationToken cancellationToken)
    {
        await sender.Send(new LoadSampleDataCommand(), cancellationToken);
        return NoContent();
    }

    [HttpDelete("sample-data")]
    [Authorize(Roles = "Owner,Administrator")]
    public async Task<IActionResult> RemoveSampleData(CancellationToken cancellationToken)
    {
        await sender.Send(new RemoveSampleDataCommand(), cancellationToken);
        return NoContent();
    }

    [HttpPost("export")]
    [Authorize(Roles = "Owner")]
    public async Task<IActionResult> Export(CancellationToken cancellationToken)
    {
        var content = await exportService.CreateArchiveAsync(cancellationToken);
        var job = new ReportExportJob
        {
            ReportType = "tenant",
            Format = "zip",
            Status = ReportExportStatus.Completed,
            FileName = "inventoryx-export.zip",
            ContentType = "application/zip",
            Content = content,
            CompletedAt = DateTime.UtcNow,
        };
        context.ReportExportJobs.Add(job);
        await context.SaveChangesAsync(cancellationToken);
        return AcceptedAtAction(nameof(GetExport), new { jobId = job.Id }, new { jobId = job.Id, status = "Completed" });
    }

    [HttpGet("export/{jobId:guid}")]
    [Authorize(Roles = "Owner")]
    public async Task<IActionResult> GetExport(Guid jobId, CancellationToken cancellationToken)
    {
        var job = await context.ReportExportJobs.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == jobId && item.ReportType == "tenant", cancellationToken);
        return job?.Content is null ? NotFound() : File(job.Content, job.ContentType!, job.FileName);
    }
}
