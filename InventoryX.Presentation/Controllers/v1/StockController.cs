using InventoryX.Application.Commands.Requests.Inventory;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryX.Presentation.Controllers.v1;

[Route("api/v1/stock")]
[Authorize]
public sealed class StockController(ISender sender) : ApiControllerBase
{
    [HttpPost("adjustments")]
    public async Task<ActionResult<RecordStockAdjustmentResult>> RecordAdjustment(
        RecordStockAdjustmentCommand command,
        CancellationToken cancellationToken) =>
        Ok(await sender.Send(command, cancellationToken));
}
