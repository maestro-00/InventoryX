using InventoryX.Domain.Models.Common;

namespace InventoryX.Domain.Models.Tenancy;

public enum PaymentMethodKind { Card, MobileMoney }
public enum PaymentRenewalMode { Automatic, OwnerApproval }
public enum PaymentMethodStatus { Pending, Active, Retired, Invalid }

/// <summary>Safe summary and encrypted provider authorization; no card credentials.</summary>
public sealed class PaymentMethod : BaseModel
{
    public PaymentMethodKind Kind { get; set; }
    public PaymentRenewalMode RenewalMode { get; set; }
    public PaymentMethodStatus Status { get; private set; } = PaymentMethodStatus.Pending;
    public string Provider { get; set; } = string.Empty;
    public string MaskedIdentifier { get; set; } = string.Empty;
    public string? Brand { get; set; }
    public int? ExpiryMonth { get; set; }
    public int? ExpiryYear { get; set; }
    public string? EncryptedAuthorizationCode { get; set; }
    public string? EncryptedPhoneNumber { get; set; }
    public DateTime? VerifiedAt { get; private set; }
    public DateTime? ReplacedAt { get; private set; }

    public void Activate(DateTime verifiedAt)
    {
        if (Status == PaymentMethodStatus.Retired) throw new InvalidOperationException("A retired method cannot be reactivated.");
        if (Kind == PaymentMethodKind.MobileMoney && RenewalMode != PaymentRenewalMode.OwnerApproval)
            throw new InvalidOperationException("Mobile money requires approval for every charge.");
        if (Kind == PaymentMethodKind.MobileMoney && EncryptedAuthorizationCode is not null)
            throw new InvalidOperationException("Mobile money authorizations cannot be reused.");
        if (Kind == PaymentMethodKind.Card &&
            (EncryptedAuthorizationCode is null || !EncryptedAuthorizationCode.StartsWith("v1:", StringComparison.Ordinal)))
            throw new InvalidOperationException("A reusable card authorization must be verified first.");
        if (Kind == PaymentMethodKind.MobileMoney &&
            (EncryptedPhoneNumber is null || !EncryptedPhoneNumber.StartsWith("v1:", StringComparison.Ordinal)))
            throw new InvalidOperationException("A protected mobile money number is required.");
        Status = PaymentMethodStatus.Active;
        VerifiedAt = verifiedAt;
    }

    public void Retire(DateTime replacedAt)
    {
        Status = PaymentMethodStatus.Retired;
        ReplacedAt = replacedAt;
    }
}
