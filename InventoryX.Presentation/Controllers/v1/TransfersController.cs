using InventoryX.Application.Commands.Requests.Inventory;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryX.Presentation.Controllers.v1;

[Route("api/v1/transfers")]
[Authorize]
public sealed class TransfersController(ISender sender) : ApiControllerBase
{
    [HttpPost]
    public async Task<ActionResult<StockTransferResult>> Create(CreateStockTransferCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(Create), new { id = result.Id }, result);
    }

    [HttpPost("{id:guid}/dispatch")]
    public Task<StockTransferResult> Dispatch(Guid id, CancellationToken cancellationToken) =>
        sender.Send(new DispatchStockTransferCommand { TransferId = id }, cancellationToken);

    [HttpPost("{id:guid}/receive")]
    public Task<StockTransferResult> Receive(Guid id, ReceiveStockTransferCommand command, CancellationToken cancellationToken) =>
        sender.Send(new ReceiveStockTransferCommand
        { TransferId = id, Lines = command.Lines, DiscrepancyReason = command.DiscrepancyReason }, cancellationToken);
}
