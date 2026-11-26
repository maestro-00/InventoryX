using InventoryX.Application.Queries.Requests.Billing;
using InventoryX.Application.Commands.Requests.Billing;
using InventoryX.Application.DTOs.Common;
using MediatR;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryX.Presentation.Controllers.v1;

[Route("api/v1/billing")]
[Authorize]
[Tags("Billing")]
public sealed class BillingController(ISender sender) : ApiControllerBase
{
    [HttpGet("plans")]
    [AllowAnonymous]
    public async Task<ActionResult<List<BillingPlanDto>>> Plans(CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetBillingPlansQuery(), cancellationToken));

    [HttpGet("subscription")]
    [Authorize(Roles = "Owner")]
    public async Task<ActionResult<BillingSubscriptionDto>> Subscription(CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetCurrentBillingSubscriptionQuery(), cancellationToken));

    [HttpPost("subscription/upgrade")]
    [Authorize(Roles = "Owner")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status410Gone)]
    public IActionResult Upgrade() => BillingUpgradeRequired();

    [HttpPost("subscription/downgrade")]
    [Authorize(Roles = "Owner")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status410Gone)]
    public IActionResult Downgrade() => BillingUpgradeRequired();

    [HttpPost("subscription/cancel")]
    [Authorize(Roles = "Owner")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status410Gone)]
    public IActionResult Cancel() => BillingUpgradeRequired();

    [HttpPost("subscription/reactivate")]
    [Authorize(Roles = "Owner")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status410Gone)]
    public IActionResult Reactivate() => BillingUpgradeRequired();

    [HttpPost("payment-method")]
    [Authorize(Roles = "Owner")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status410Gone)]
    public IActionResult PaymentMethod() => BillingUpgradeRequired();

    [HttpGet("invoices")]
    [Authorize(Roles = "Owner")]
    [ProducesResponseType(typeof(PagedResult<BillingInvoiceDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<BillingInvoiceDto>>> Invoices(
        [FromQuery] GetBillingInvoicesQuery query,
        CancellationToken cancellationToken) =>
        Ok(await sender.Send(query, cancellationToken));

    [HttpGet("invoices/{id:guid}/pdf")]
    [Authorize(Roles = "Owner")]
    public async Task<IActionResult> InvoicePdf(Guid id, CancellationToken cancellationToken)
    {
        var invoice = await sender.Send(new GetBillingInvoicePdfQuery(id), cancellationToken);
        return File(Encoding.UTF8.GetBytes(invoice.Content), "application/pdf", $"{invoice.Number}.pdf");
    }

    [HttpPatch("contact")]
    [Authorize(Roles = "Owner")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Contact(UpdateBillingContactCommand command, CancellationToken cancellationToken)
    {
        await sender.Send(command, cancellationToken);
        return NoContent();
    }

    private IActionResult BillingUpgradeRequired()
    {
        var details = new ProblemDetails
        {
            Status = StatusCodes.Status410Gone,
            Title = "Billing API upgrade required",
            Detail = "Update the billing client to use the v2 payment flow.",
            Type = "https://httpstatuses.com/410",
            Instance = HttpContext?.Request.Path.Value,
        };
        details.Extensions["code"] = "billing_api_upgrade_required";
        details.Extensions["migrationUrl"] = "/api/v2/billing";
        return new ObjectResult(details)
        {
            StatusCode = StatusCodes.Status410Gone,
            ContentTypes = { "application/problem+json" },
        };
    }
}
