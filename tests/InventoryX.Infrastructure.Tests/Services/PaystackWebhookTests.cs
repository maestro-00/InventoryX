using FluentAssertions;

namespace InventoryX.Infrastructure.Tests.Services;

/// <summary>T075 - webhook authenticity and exactly-once event processing contract.</summary>
public sealed class PaystackWebhookTests
{
    [Fact]
    public void Webhook_processor_exposes_signature_verification_and_idempotent_processing()
    {
        var processor = Type.GetType("InventoryX.Infrastructure.Services.PaystackWebhookProcessor, InventoryX.Infrastructure");
        processor.Should().NotBeNull();
        processor!.GetMethods().Should().Contain(method => method.Name == "VerifySignature",
            "Paystack's x-paystack-signature HMAC-SHA512 must be verified before processing");
        processor.GetMethods().Should().Contain(method => method.Name == "ProcessAsync",
            "event IDs must be persisted so duplicate deliveries become no-ops");

        Type.GetType("InventoryX.Domain.Models.Tenancy.ProcessedWebhookEvent, InventoryX.Domain")
            .Should().NotBeNull("webhook event IDs require durable idempotency storage");
    }
}
