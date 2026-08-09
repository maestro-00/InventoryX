using InventoryX.Application.Commands.Requests.Import;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryX.Presentation.Controllers.v1;

[Route("api/v1/import")]
[Authorize]
public sealed class ImportController(ISender sender) : ApiControllerBase
{
    public sealed record CommitImportRequest(Guid? LocationId);

    [HttpPost("products")]
    public Task<ActionResult<ImportJobDto>> UploadProducts(IFormFile file, CancellationToken cancellationToken) =>
        Upload(file, "Products", cancellationToken);

    [HttpPost("opening-stock")]
    public Task<ActionResult<ImportJobDto>> UploadOpeningStock(IFormFile file, CancellationToken cancellationToken) =>
        Upload(file, "OpeningStock", cancellationToken);

    [HttpPut("products/{jobId:guid}/mapping")]
    [HttpPut("opening-stock/{jobId:guid}/mapping")]
    public async Task<ActionResult<ImportJobDto>> SetMapping(
        Guid jobId,
        Dictionary<string, string> columnMapping,
        CancellationToken cancellationToken) =>
        Ok(await sender.Send(new SetImportMappingCommand
        {
            JobId = jobId,
            ColumnMapping = columnMapping,
        }, cancellationToken));

    [HttpPost("products/{jobId:guid}/commit")]
    [HttpPost("opening-stock/{jobId:guid}/commit")]
    public async Task<ActionResult<ImportJobDto>> Commit(
        Guid jobId,
        CommitImportRequest? request,
        CancellationToken cancellationToken) =>
        Ok(await sender.Send(new CommitImportCommand
        {
            JobId = jobId,
            LocationId = request?.LocationId,
        }, cancellationToken));

    [HttpDelete("products/{jobId:guid}")]
    [HttpDelete("opening-stock/{jobId:guid}")]
    public async Task<IActionResult> Abandon(Guid jobId, CancellationToken cancellationToken)
    {
        await sender.Send(new AbandonImportCommand { JobId = jobId }, cancellationToken);
        return NoContent();
    }

    private async Task<ActionResult<ImportJobDto>> Upload(
        IFormFile file,
        string kind,
        CancellationToken cancellationToken)
    {
        await using var stream = new MemoryStream();
        await file.CopyToAsync(stream, cancellationToken);
        var result = await sender.Send(new CreateImportJobCommand
        {
            Kind = kind,
            FileName = file.FileName,
            FileContent = stream.ToArray(),
        }, cancellationToken);
        return CreatedAtAction(nameof(SetMapping), new { jobId = result.Id }, result);
    }
}
