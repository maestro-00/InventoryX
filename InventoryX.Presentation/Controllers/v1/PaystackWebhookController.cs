using System.Text;
using InventoryX.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace InventoryX.Presentation.Controllers.v1;

[ApiController]
[AllowAnonymous]
[Route("api/v1/billing/webhooks/paystack")]
[EnableRateLimiting("webhook")]
public sealed class PaystackWebhookController(PaystackWebhookProcessor processor) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Receive(CancellationToken cancellationToken)
    {
        await using var buffer = new MemoryStream();
        await Request.Body.CopyToAsync(buffer, cancellationToken);
        var rawBody = buffer.ToArray();
        var signatureMode = processor.VerifySignature(rawBody,
            Request.Headers["Paystack-Signature"].FirstOrDefault(),
            Request.Headers["x-paystack-signature"].FirstOrDefault());
        if (signatureMode is null)
            return Unauthorized();
        await processor.EnqueueAsync(Encoding.UTF8.GetString(rawBody), signatureMode.Value, cancellationToken);
        return Ok();
    }
}
