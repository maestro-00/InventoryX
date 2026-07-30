using InventoryX.Application.Commands.Requests.Selling;
using InventoryX.Application.DTOs.Selling;
using InventoryX.Application.Queries.Requests.Selling;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryX.Presentation.Controllers.v1;

[Route("api/v1/registers")]
[Authorize]
public sealed class RegistersController(ISender sender) : ApiControllerBase
{
    public sealed record OpenShiftRequest(decimal OpeningFloat);
    public sealed record FavouritesRequest(string LayoutJson);

    [HttpGet]
    public async Task<ActionResult<List<RegisterDto>>> List(
        [FromQuery] Guid? locationId,
        CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetRegistersQuery { LocationId = locationId }, cancellationToken));

    [HttpPost]
    public async Task<ActionResult<RegisterDto>> Create(
        CreateRegisterCommand command,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(List), new { locationId = result.LocationId }, result);
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
        return CreatedAtAction(nameof(List), new { locationId = result.RegisterId }, result);
    }

    [HttpGet("shifts/{shiftId:guid}/z-report")]
    public async Task<ActionResult<ZReportDto>> ZReport(Guid shiftId, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetZReportQuery(shiftId), cancellationToken));

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
