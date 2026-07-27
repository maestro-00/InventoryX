using InventoryX.Application.Commands.Requests.Tenancy;
using InventoryX.Application.Queries.Requests.Tenancy;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryX.Presentation.Controllers.v1;

[Route("api/v1/tenant")]
[Authorize]
public sealed class TenantController(ISender sender) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<TenantDto>> Get(CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetTenantQuery(), cancellationToken));

    [HttpPatch]
    [Authorize(Roles = "Owner,Administrator")]
    public async Task<ActionResult<TenantDto>> Update(
        UpdateTenantCommand command,
        CancellationToken cancellationToken) =>
        Ok(await sender.Send(command, cancellationToken));

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
}
