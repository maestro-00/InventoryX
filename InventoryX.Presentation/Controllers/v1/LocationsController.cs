using InventoryX.Application.Commands.Requests.Inventory;
using InventoryX.Application.DTOs.Inventory;
using InventoryX.Application.Queries.Requests.Inventory;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryX.Presentation.Controllers.v1;

[Route("api/v1/locations")]
[Authorize]
public sealed class LocationsController(ISender sender) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<LocationDto>>> List(CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetLocationsQuery(), cancellationToken));

    [HttpPost]
    public async Task<ActionResult<LocationDto>> Create(CreateLocationCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(List), new { id = result.Id }, result);
    }

    [HttpPatch("{id:guid}")]
    public async Task<ActionResult<LocationDto>> Update(Guid id, UpdateLocationCommand command, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new UpdateLocationCommand
        {
            Id = id,
            Name = command.Name,
            Address = command.Address,
            Kind = command.Kind,
            IsActive = command.IsActive,
        }, cancellationToken));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new DeleteLocationCommand { Id = id }, cancellationToken);
        return NoContent();
    }
}
