using InventoryX.Application.Commands.Requests.Selling;
using InventoryX.Application.DTOs.Selling;
using InventoryX.Application.Queries.Requests.Selling;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryX.Presentation.Controllers.v1;

[Route("api/v1/registers")]
[Authorize]
[Tags("Registers")]
public sealed class RegistersController(ISender sender) : ApiControllerBase
{
    public sealed record CreateRegisterRequest(Guid LocationId, string Name);
    public sealed record UpdateRegisterRequest(string? Name, bool? IsActive);
    public sealed record OpenShiftRequest(decimal OpeningFloat);
    public sealed record FavouritesRequest(string LayoutJson);

    [HttpGet]
    public async Task<ActionResult<List<RegisterDto>>> List(
        [FromQuery] Guid? locationId,
        CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetRegistersQuery { LocationId = locationId }, cancellationToken));

    [HttpPost]
    [ProducesResponseType(typeof(RegisterDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status402PaymentRequired)]
    public async Task<ActionResult<RegisterDto>> Create(
        CreateRegisterRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CreateRegisterCommand
        {
            LocationId = request.LocationId,
            Name = request.Name,
        }, cancellationToken);
        SetETag(result.RowVersion);
        return CreatedAtAction(nameof(List), new { locationId = result.LocationId }, result);
    }

    [HttpPatch("{id:guid}")]
    [ProducesResponseType(typeof(RegisterDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RegisterDto>> Update(
        Guid id,
        UpdateRegisterRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new UpdateRegisterCommand
        {
            Id = id,
            Name = request.Name,
            IsActive = request.IsActive,
            ExpectedRowVersion = ParseIfMatchRowVersion(),
        }, cancellationToken);
        SetETag(result.RowVersion);
        return Ok(result);
    }

    [HttpPost("{registerId:guid}/shifts")]
    public async Task<ActionResult<ShiftDto>> OpenShift(
        Guid registerId,
        OpenShiftRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new OpenShiftCommand
        {
            RegisterId = registerId,
            OpeningFloat = request.OpeningFloat,
        }, cancellationToken);
        return CreatedAtAction(nameof(ListShifts), new { registerId }, result);
    }

    [HttpGet("{registerId:guid}/shifts")]
    public async Task<ActionResult<List<ShiftDto>>> ListShifts(
        Guid registerId,
        [FromQuery] string? status,
        CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetShiftsQuery { RegisterId = registerId, Status = status }, cancellationToken));

    [HttpGet("{registerId:guid}/favourites")]
    public async Task<ActionResult<FavouritesLayoutDto>> GetFavourites(
        Guid registerId,
        CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetFavouritesLayoutQuery { RegisterId = registerId }, cancellationToken));

    [HttpPut("{registerId:guid}/favourites")]
    public async Task<ActionResult<FavouritesLayoutDto>> PutFavourites(
        Guid registerId,
        FavouritesRequest request,
        CancellationToken cancellationToken) =>
        Ok(await sender.Send(new UpsertFavouritesLayoutCommand
        {
            RegisterId = registerId,
            LayoutJson = request.LayoutJson,
        }, cancellationToken));
}
