using InventoryX.Application.Commands.Requests.Inventory;
using InventoryX.Application.Queries.Requests.Inventory;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryX.Presentation.Controllers.v1;

[Route("api/v1/counts")]
[Authorize]
public sealed class CountsController(ISender sender) : ApiControllerBase
{
    [HttpPost]
    public async Task<ActionResult<StockCountResult>> Open(OpenStockCountCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<StockCountResult>> Get(Guid id, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetStockCountQuery(id), cancellationToken));

    [HttpPut("{id:guid}/lines")]
    public async Task<ActionResult<StockCountResult>> Lines(Guid id, UpdateStockCountLinesCommand command, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new UpdateStockCountLinesCommand { CountId = id, Lines = command.Lines }, cancellationToken));

    [HttpPost("{id:guid}/submit")]
    public Task<StockCountResult> Submit(Guid id, CancellationToken cancellationToken) =>
        sender.Send(new SubmitStockCountCommand { CountId = id }, cancellationToken);

    [HttpPost("{id:guid}/approve")]
    [Authorize(Roles = "Owner,Administrator,Manager")]
    public Task<StockCountResult> Approve(Guid id, CancellationToken cancellationToken) =>
        sender.Send(new ApproveStockCountCommand { CountId = id }, cancellationToken);

    [HttpPost("{id:guid}/reject")]
    [Authorize(Roles = "Owner,Administrator,Manager")]
    public Task<StockCountResult> Reject(Guid id, CancellationToken cancellationToken) =>
        sender.Send(new RejectStockCountCommand { CountId = id }, cancellationToken);
}
