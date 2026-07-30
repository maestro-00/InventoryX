using System.Text;
using InventoryX.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryX.Presentation.Controllers.v1;

[ApiController]
[AllowAnonymous]
[Route("api/v1/billing/webhooks/paystack")]
public sealed class PaystackWebhookController(PaystackWebhookProcessor processor) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Receive(CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(Request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: false);
        var payload = await reader.ReadToEndAsync(cancellationToken);
        if (!processor.VerifySignature(payload, Request.Headers["x-paystack-signature"].FirstOrDefault()))
            return Unauthorized();
        await processor.ProcessAsync(payload, cancellationToken);
        return Ok();
    }
}
