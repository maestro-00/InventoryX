using InventoryX.Application.Queries.Requests.Exports;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryX.Presentation.Controllers.v1;

[Route("api/v1/export")]
[Authorize]
public sealed class ExportController(ISender sender) : ApiControllerBase
{
    private bool CanViewCost =>
        User.IsInRole("Owner") || User.IsInRole("Administrator") || User.IsInRole("Manager");

    [HttpGet("products")]
    public Task<IActionResult> Products([FromQuery] string format = "csv", CancellationToken cancellationToken = default) =>
        Export("products", format, cancellationToken);

    [HttpGet("stock")]
    public Task<IActionResult> Stock([FromQuery] string format = "csv", CancellationToken cancellationToken = default) =>
        Export("stock", format, cancellationToken);

    private async Task<IActionResult> Export(string resource, string format, CancellationToken cancellationToken)
    {
        var document = await sender.Send(new ExportCatalogueQuery(resource, format, CanViewCost), cancellationToken);
        return File(document.Content, document.ContentType, document.FileName);
    }
}
