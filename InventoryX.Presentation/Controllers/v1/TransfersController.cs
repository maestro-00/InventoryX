using InventoryX.Application.Commands.Requests.Inventory;
using InventoryX.Application.DTOs.Common;
using InventoryX.Application.Queries.Requests.Inventory;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryX.Presentation.Controllers.v1;

[Route("api/v1/transfers")]
[Authorize]
[Tags("Transfers")]
public sealed class TransfersController(ISender sender) : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<StockTransferDto>), StatusCodes.Status200OK)]
    public Task<PagedResult<StockTransferDto>> List(
        [FromQuery] GetTransfersQuery query,
        CancellationToken cancellationToken) =>
        sender.Send(query, cancellationToken);

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
