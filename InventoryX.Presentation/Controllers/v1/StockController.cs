using InventoryX.Application.Commands.Requests.Inventory;
using InventoryX.Application.DTOs.Common;
using InventoryX.Application.DTOs.Inventory;
using InventoryX.Application.Queries.Requests.Inventory;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryX.Presentation.Controllers.v1;

[Route("api/v1/stock")]
[Authorize]
public sealed class StockController(ISender sender) : ApiControllerBase
{
    private bool CanViewProfit =>
        User.IsInRole("Owner") || User.IsInRole("Administrator") || User.IsInRole("Manager");

    [HttpGet]
    public async Task<ActionResult<PagedResult<StockLevelDto>>> List(
        [FromQuery] GetStockQuery query,
        CancellationToken cancellationToken) =>
        Ok(await sender.Send(query with { IncludeCost = CanViewProfit }, cancellationToken));

    [HttpPost("adjustments")]
    public async Task<ActionResult<RecordStockAdjustmentResult>> RecordAdjustment(
        RecordStockAdjustmentCommand command,
        CancellationToken cancellationToken) =>
        Ok(await sender.Send(command, cancellationToken));
}
