using InventoryX.Application.Queries.Requests.Billing;
using InventoryX.Application.Commands.Requests.Billing;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryX.Presentation.Controllers.v1;

[Route("api/v1/billing")]
[Authorize]
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
    public async Task<ActionResult<BillingSubscriptionDto>> Upgrade(UpgradeSubscriptionCommand command, CancellationToken cancellationToken) =>
        Ok(await sender.Send(command, cancellationToken));

    [HttpPost("subscription/downgrade")]
    [Authorize(Roles = "Owner")]
    public async Task<ActionResult<BillingSubscriptionDto>> Downgrade(DowngradeSubscriptionCommand command, CancellationToken cancellationToken) =>
        Ok(await sender.Send(command, cancellationToken));

    [HttpPost("subscription/cancel")]
    [Authorize(Roles = "Owner")]
    public async Task<ActionResult<BillingSubscriptionDto>> Cancel(CancellationToken cancellationToken) =>
        Ok(await sender.Send(new CancelSubscriptionCommand(), cancellationToken));

    [HttpPost("subscription/reactivate")]
    [Authorize(Roles = "Owner")]
    public async Task<ActionResult<BillingSubscriptionDto>> Reactivate(CancellationToken cancellationToken) =>
        Ok(await sender.Send(new ReactivateSubscriptionCommand(), cancellationToken));

    [HttpPost("payment-method")]
    [Authorize(Roles = "Owner")]
    public async Task<ActionResult<InventoryX.Application.Services.IServices.PaymentInitializationResult>> PaymentMethod(
        InitializePaymentMethodCommand command,
        CancellationToken cancellationToken) =>
        Ok(await sender.Send(command, cancellationToken));
}
