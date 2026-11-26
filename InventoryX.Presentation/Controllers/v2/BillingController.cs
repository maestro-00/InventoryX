using InventoryX.Application.DTOs.Common;
using InventoryX.Application.Commands.Requests.Billing;
using InventoryX.Application.Queries.Requests.Billing;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryX.Presentation.Controllers.v2;

[ApiController]
[Route("api/v2/billing")]
[Authorize]
[Tags("Billing v2")]
public sealed class BillingController(ISender sender) : ControllerBase
{
    [HttpGet("subscription")]
    [Authorize(Roles = "Owner")]
    [ProducesResponseType(typeof(BillingSubscriptionDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<BillingSubscriptionDto>> Subscription(CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetCurrentBillingSubscriptionQuery(), cancellationToken));

    [HttpGet("invoices")]
    [Authorize(Roles = "Owner")]
    [ProducesResponseType(typeof(PagedResult<BillingInvoiceDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<BillingInvoiceDto>>> Invoices(
        [FromQuery] GetBillingInvoicesQuery query, CancellationToken cancellationToken) =>
        Ok(await sender.Send(query, cancellationToken));

    [HttpPatch("contact")]
    [Authorize(Roles = "Owner")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Contact(
        [FromBody] UpdateBillingContactCommand command,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
            return Problem(statusCode: 400, title: "Idempotency-Key is required.");
        await sender.Send(command with { IdempotencyKey = idempotencyKey }, cancellationToken);
        return NoContent();
    }
}
