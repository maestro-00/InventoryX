using InventoryX.Application.Services.IServices;
using InventoryX.Domain.Models.Selling;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Logging;

namespace InventoryX.Infrastructure.Services
{
    /// <summary>
    /// Delivers receipts via email (SendGrid). SMS and QR delivery
    /// paths are scaffolded for future cycles.
    /// </summary>
    public sealed class ReceiptDeliveryService(
        IEmailSender emailSender,
        ILogger<ReceiptDeliveryService> logger) : IReceiptDeliveryService
    {
        public async Task DeliverAsync(Receipt receipt, ReceiptChannel channel, string destination, CancellationToken cancellationToken)
        {
            switch (channel)
            {
                case ReceiptChannel.Email:
                    var subject = $"Receipt {receipt.Number}";
                    var body = $"<pre>{receipt.PayloadJson}</pre>";
                    await emailSender.SendEmailAsync(destination, subject, body);
                    break;

                case ReceiptChannel.Sms:
                case ReceiptChannel.Qr:
                    logger.LogInformation(
                        "Receipt {Number} channel {Channel} delivery to {Dest} — not yet implemented in Cycle 1.",
                        receipt.Number, channel, destination);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(channel), channel, "Unknown delivery channel.");
            }
        }
    }
}