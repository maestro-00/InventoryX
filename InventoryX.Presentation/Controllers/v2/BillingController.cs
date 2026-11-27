using InventoryX.Application.DTOs.Common;
using InventoryX.Application.Commands.Requests.Billing;
using InventoryX.Application.Commands.RequestHandlers.Billing;
using InventoryX.Application.Queries.Requests.Billing;
using InventoryX.Domain.Models.Tenancy;
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
    /// <summary>Preview the price and timing of a plan purchase.</summary>
    [HttpPost("quotes")]
    [Authorize(Roles = "Owner")]
    [ProducesResponseType(typeof(BillingQuoteDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BillingQuoteDto>> Quote(
        [FromBody] CreateBillingQuoteRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
            return Problem(statusCode: 400, title: "Idempotency-Key is required.");
        return Ok(await sender.Send(new QuoteBillingCommand(request.TargetPlanId,
            request.BillingCycle, request.Purpose, idempotencyKey), cancellationToken));
    }

    /// <summary>Start hosted card checkout or a Ghana mobile money approval.</summary>
    [HttpPost("payment-intents")]
    [Authorize(Roles = "Owner")]
    [ProducesResponseType(typeof(CreatePaymentIntentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(CreatePaymentIntentDto), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status402PaymentRequired)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CreatePaymentIntentDto>> Purchase(
        [FromBody] CreateBillingPurchaseRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
            return Problem(statusCode: 400, title: "Idempotency-Key is required.");
        var result = await sender.Send(new CreatePaymentIntentCommand(request.QuoteId,
            request.MethodKind, request.Provider, request.PhoneNumber, idempotencyKey), cancellationToken);
        if (result.Status == "failed")
        {
            var problem = new ProblemDetails
            {
                Title = "Payment was not approved.", Status = StatusCodes.Status402PaymentRequired,
                Detail = "Choose another payment method or retry with a fresh quote.",
            };
            problem.Extensions["code"] = "payment_failed";
            problem.Extensions["intentId"] = result.IntentId;
            return StatusCode(StatusCodes.Status402PaymentRequired, problem);
        }
        if (result.Status == "awaiting_approval")
            return AcceptedAtAction(nameof(PaymentIntentStatus), new { id = result.IntentId }, result);
        return CreatedAtAction(nameof(PaymentIntentStatus), new { id = result.IntentId }, result);
    }

    [HttpGet("plans")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<BillingPlanDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<BillingPlanDto>>> Plans(CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetBillingPlansQuery(), cancellationToken));

    [HttpGet("payment-intents/{id:guid}")]
    [Authorize(Roles = "Owner")]
    [ProducesResponseType(typeof(PaymentIntentStatusDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PaymentIntentStatusDto>> PaymentIntentStatus(
        Guid id, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetPaymentIntentStatusQuery(id), cancellationToken));

    [HttpGet("subscription")]
    [Authorize(Roles = "Owner")]
    [ProducesResponseType(typeof(BillingSubscriptionDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<BillingSubscriptionDto>> Subscription(CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetCurrentBillingSubscriptionQuery(), cancellationToken));

    [HttpGet("payment-methods")]
    [Authorize(Roles = "Owner")]
    [ProducesResponseType(typeof(List<BillingPaymentMethodDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<BillingPaymentMethodDto>>> PaymentMethods(CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetBillingPaymentMethodsQuery(), cancellationToken));

    [HttpGet("payment-methods/quote")]
    [Authorize(Roles = "Owner")]
    [ProducesResponseType(typeof(MethodVerificationQuoteDto), StatusCodes.Status200OK)]
    public ActionResult<MethodVerificationQuoteDto> MethodVerificationQuote() => Ok(
        new MethodVerificationQuoteDto(InitializePaymentMethodCommandHandler.VerificationAmount,
            0m, InitializePaymentMethodCommandHandler.VerificationAmount, "GHS"));

    [HttpPost("payment-methods")]
    [Authorize(Roles = "Owner")]
    [ProducesResponseType(typeof(InitializePaymentMethodResult), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<InitializePaymentMethodResult>> ReplacePaymentMethod(
        [FromBody] ReplacePaymentMethodRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
            return Problem(statusCode: 400, title: "Idempotency-Key is required.");
        var result = await sender.Send(new InitializePaymentMethodCommand
        {
            Channel = request.Channel, Provider = request.Provider, Msisdn = request.Msisdn,
            IdempotencyKey = idempotencyKey,
        }, cancellationToken);
        return AcceptedAtAction(nameof(PaymentIntentStatus), new { id = result.IntentId }, result);
    }

    [HttpGet("invoices")]
    [Authorize(Roles = "Owner")]
    [ProducesResponseType(typeof(PagedResult<BillingInvoiceDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<BillingInvoiceDto>>> Invoices(
        [FromQuery] GetBillingInvoicesQuery query, CancellationToken cancellationToken) =>
        Ok(await sender.Send(query, cancellationToken));

    [HttpGet("invoices/{id:guid}/pdf")]
    [Authorize(Roles = "Owner")]
    [Produces("application/pdf")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK, "application/pdf")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> InvoicePdf(Guid id, CancellationToken cancellationToken)
    {
        var invoice = await sender.Send(new GetBillingInvoicePdfQuery(id), cancellationToken);
        return File(Convert.FromBase64String(invoice.Content), "application/pdf", $"{invoice.Number}.pdf");
    }

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

    [HttpPost("subscription/upgrade")]
    [Authorize(Roles = "Owner")]
    [ProducesResponseType(typeof(BillingSubscriptionDto), StatusCodes.Status200OK)]
    public Task<BillingSubscriptionDto> Upgrade([FromBody] UpgradeSubscriptionCommand command,
        [FromHeader(Name = "Idempotency-Key")] string idempotencyKey, CancellationToken cancellationToken)
    {
        command.IdempotencyKey = idempotencyKey;
        return sender.Send(command, cancellationToken);
    }

    [HttpPost("subscription/downgrade")]
    [Authorize(Roles = "Owner")]
    [ProducesResponseType(typeof(BillingSubscriptionDto), StatusCodes.Status200OK)]
    public Task<BillingSubscriptionDto> Downgrade([FromBody] DowngradeSubscriptionCommand command,
        [FromHeader(Name = "Idempotency-Key")] string idempotencyKey, CancellationToken cancellationToken)
    {
        command.IdempotencyKey = idempotencyKey;
        return sender.Send(command, cancellationToken);
    }

    [HttpPost("subscription/cancel")]
    [Authorize(Roles = "Owner")]
    [ProducesResponseType(typeof(BillingSubscriptionDto), StatusCodes.Status200OK)]
    public Task<BillingSubscriptionDto> Cancel([FromHeader(Name = "Idempotency-Key")] string idempotencyKey,
        CancellationToken cancellationToken) =>
        sender.Send(new CancelSubscriptionCommand { IdempotencyKey = idempotencyKey }, cancellationToken);

    [HttpPost("subscription/reactivate")]
    [Authorize(Roles = "Owner")]
    [ProducesResponseType(typeof(BillingSubscriptionDto), StatusCodes.Status200OK)]
    public Task<BillingSubscriptionDto> Reactivate([FromHeader(Name = "Idempotency-Key")] string idempotencyKey,
        CancellationToken cancellationToken) =>
        sender.Send(new ReactivateSubscriptionCommand { IdempotencyKey = idempotencyKey }, cancellationToken);

    [HttpPost("changes")]
    [Authorize(Roles = "Owner")]
    [ProducesResponseType(typeof(BillingSubscriptionDto), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BillingSubscriptionDto>> ScheduleChange(
        [FromBody] ScheduleSubscriptionChangeCommand command,
        [FromHeader(Name = "Idempotency-Key")] string idempotencyKey, CancellationToken cancellationToken)
    {
        command.IdempotencyKey = idempotencyKey;
        return Accepted(await sender.Send(command, cancellationToken));
    }

    [HttpDelete("changes/{id:guid}")]
    [Authorize(Roles = "Owner")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ReverseChange(Guid id, [FromHeader(Name = "Idempotency-Key")] string idempotencyKey, CancellationToken cancellationToken)
    {
        await sender.Send(new ReverseSubscriptionChangeCommand(id) { IdempotencyKey = idempotencyKey }, cancellationToken);
        return NoContent();
    }

    [HttpDelete("cancellation")]
    [Authorize(Roles = "Owner")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ReverseCancellation([FromHeader(Name = "Idempotency-Key")] string idempotencyKey, CancellationToken cancellationToken)
    {
        await sender.Send(new ReverseSubscriptionChangeCommand() { IdempotencyKey = idempotencyKey }, cancellationToken);
        return NoContent();
    }

    [HttpPost("cancellation")]
    [Authorize(Roles = "Owner")]
    [ProducesResponseType(typeof(BillingSubscriptionDto), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BillingSubscriptionDto>> ScheduleCancellation(
        [FromHeader(Name = "Idempotency-Key")] string idempotencyKey, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CancelSubscriptionCommand { IdempotencyKey = idempotencyKey }, cancellationToken);
        return Accepted(result);
    }

    [HttpPost("recovery")]
    [Authorize(Roles = "Owner")]
    [ProducesResponseType(typeof(CreatePaymentIntentDto), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status402PaymentRequired)]
    public async Task<ActionResult<CreatePaymentIntentDto>> Recovery(
        [FromBody] RecoveryRequest request, [FromHeader(Name = "Idempotency-Key")] string idempotencyKey,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new StartRecoveryCommand(request.MethodKind, request.Provider, request.PhoneNumber, idempotencyKey), cancellationToken);
        return AcceptedAtAction(nameof(PaymentIntentStatus), new { id = result.IntentId }, result);
    }
}

/// <summary>Published plan and cycle to preview before payment.</summary>
public sealed record CreateBillingQuoteRequest(Guid TargetPlanId, BillingCycle BillingCycle,
    PaymentIntentPurpose Purpose);

/// <summary>Confirmed quote and payment channel for a first purchase.</summary>
public sealed record CreateBillingPurchaseRequest(Guid QuoteId, string MethodKind,
    string? Provider, string? PhoneNumber);

public sealed record ReplacePaymentMethodRequest(string Channel, string? Provider, string? Msisdn);
public sealed record RecoveryRequest(string MethodKind, string? Provider, string? PhoneNumber);
public sealed record MethodVerificationQuoteDto(decimal BaseAmount, decimal TaxAmount,
    decimal TotalAmount, string Currency);
