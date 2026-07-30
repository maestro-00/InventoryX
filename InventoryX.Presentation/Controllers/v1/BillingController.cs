using InventoryX.Application.Queries.Requests.Billing;
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
}
