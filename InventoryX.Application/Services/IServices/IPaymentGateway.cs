namespace InventoryX.Application.Services.IServices;

public record PaymentInitializationRequest(string Email, decimal Amount, string Currency = "GHS",
    string Channel = "card", string? Reference = null, string? CallbackUrl = null);
public record PaymentInitializationResult(string Reference, string AuthorizationUrl, string AccessCode);

public record PaymentChargeRequest(string Email, decimal Amount, string Currency = "GHS",
    string? AuthorizationCode = null, string? MobileMoneyProvider = null, string? Msisdn = null,
    string? Reference = null);
public record PaymentChargeResult(string Reference, string Status, string? DisplayText = null);
public record PaymentVerificationResult(string Reference, string Status, decimal Amount, string Currency,
    string? AuthorizationCode, DateTime? PaidAt);

public interface IPaymentGateway
{
    Task<PaymentInitializationResult> InitializeAuthorizationAsync(PaymentInitializationRequest request, CancellationToken cancellationToken = default);
    Task<PaymentChargeResult> ChargeAsync(PaymentChargeRequest request, CancellationToken cancellationToken = default);
    Task<PaymentVerificationResult> VerifyAsync(string reference, CancellationToken cancellationToken = default);
}
