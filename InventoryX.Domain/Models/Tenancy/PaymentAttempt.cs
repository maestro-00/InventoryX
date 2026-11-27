using InventoryX.Domain.Models.Common;

namespace InventoryX.Domain.Models.Tenancy;

public enum PaymentAttemptStatus { Created, AwaitingApproval, Unknown, Succeeded, Failed }

/// <summary>A provider request identified by a globally unique reference.</summary>
public sealed class PaymentAttempt : BaseModel
{
    public Guid PaymentIntentId { get; set; }
    public Guid? PaymentMethodId { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string? ProviderTransactionId { get; set; }
    public string? AuthorizationUrl { get; set; }
    public string? ApprovalMessage { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "GHS";
    public PaymentAttemptStatus Status { get; set; } = PaymentAttemptStatus.Created;
    public DateTime InitiatedAt { get; set; }
    public DateTime? VerifiedAt { get; private set; }
    public DateTime? SettledAt { get; private set; }
    public string? SafeFailureCode { get; private set; }

    public void MarkSucceeded(DateTime verifiedAt)
    {
        if (Status == PaymentAttemptStatus.Succeeded) return;
        Status = PaymentAttemptStatus.Succeeded;
        VerifiedAt = verifiedAt;
        SettledAt = verifiedAt;
        SafeFailureCode = null;
    }

    public void MarkFailed(DateTime verifiedAt, string? safeCode)
    {
        if (Status == PaymentAttemptStatus.Succeeded) return;
        Status = PaymentAttemptStatus.Failed;
        VerifiedAt = verifiedAt;
        SafeFailureCode = safeCode;
    }

    public void MarkUnknown()
    {
        if (Status is PaymentAttemptStatus.Created or PaymentAttemptStatus.AwaitingApproval)
            Status = PaymentAttemptStatus.Unknown;
    }
}
