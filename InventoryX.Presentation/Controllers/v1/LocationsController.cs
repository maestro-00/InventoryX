using InventoryX.Application.Commands.Requests.Inventory;
using InventoryX.Application.DTOs.Inventory;
using InventoryX.Application.Queries.Requests.Inventory;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryX.Presentation.Controllers.v1;

[Route("api/v1/locations")]
[Authorize]
[Tags("Locations")]
public sealed class LocationsController(ISender sender) : ApiControllerBase
{
    public sealed record CreateLocationRequest(string Name, string? Address, string Kind);
    public sealed record UpdateLocationRequest(string? Name, string? Address, string? Kind, bool? IsActive);

    [HttpGet]
    public async Task<ActionResult<List<LocationDto>>> List(CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetLocationsQuery(), cancellationToken));

    [HttpPost]
    [ProducesResponseType(typeof(LocationDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status402PaymentRequired)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<LocationDto>> Create(CreateLocationRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CreateLocationCommand
        {
            Name = request.Name,
            Address = request.Address,
            Kind = string.IsNullOrWhiteSpace(request.Kind) ? "Shop" : request.Kind,
        }, cancellationToken);
        SetETag(result.RowVersion);
        return CreatedAtAction(nameof(List), new { id = result.Id }, result);
    }

    [HttpPatch("{id:guid}")]
    [ProducesResponseType(typeof(LocationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LocationDto>> Update(Guid id, UpdateLocationRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new UpdateLocationCommand
        {
            Id = id,
            Name = request.Name,
            Address = request.Address,
            Kind = request.Kind,
            IsActive = request.IsActive,
            ExpectedRowVersion = ParseIfMatchRowVersion(),
        }, cancellationToken);
        SetETag(result.RowVersion);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new DeleteLocationCommand { Id = id }, cancellationToken);
        return NoContent();
    }
}
