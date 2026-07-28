using InventoryX.Application.Commands.Requests.Selling;
using InventoryX.Application.DTOs.Common;
using InventoryX.Application.DTOs.Selling;
using InventoryX.Application.Queries.Requests.Selling;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryX.Presentation.Controllers.v1;

[Route("api/v1/sales")]
[Authorize]
public sealed class SalesController(ISender sender) : ApiControllerBase
{
    public sealed record CompleteHeldSaleRequest(List<CreateSalePaymentDto> Payments);

    [HttpPost]
    public async Task<ActionResult<SaleDto>> Create(
        CreateSaleCommand command,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<SaleDto>>> List(
        [FromQuery] GetSalesQuery query,
        CancellationToken cancellationToken) =>
        Ok(await sender.Send(query, cancellationToken));

    [HttpGet("held")]
    public async Task<ActionResult<List<SaleDto>>> Held(CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetHeldSalesQuery(), cancellationToken));

    [HttpGet("{id:guid}/receipt")]
    public async Task<ActionResult<ReceiptDto>> Receipt(Guid id, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetSaleReceiptQuery(id), cancellationToken));

    [HttpGet("held/{id:guid}")]
    public async Task<ActionResult<SaleDto>> RecallHeld(Guid id, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetHeldSaleQuery { Id = id }, cancellationToken));

    [HttpPost("{id:guid}/complete")]
    public async Task<ActionResult<SaleDto>> CompleteHeld(
        Guid id,
        CompleteHeldSaleRequest request,
        CancellationToken cancellationToken) =>
        Ok(await sender.Send(new CompleteHeldSaleCommand { SaleId = id, Payments = request.Payments }, cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<SaleDto>> Get(Guid id, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetSaleQuery { Id = id }, cancellationToken));
}
