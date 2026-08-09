using InventoryX.Domain.Models.Common;

namespace InventoryX.Domain.Models.Tenancy;

/// <summary>Short staff credential, hashed independently from the full account password.</summary>
public sealed class RegisterPin : BaseModel
{
    public required string UserId { get; set; }
    public required string PasswordHash { get; set; }
    public int FailedAttempts { get; set; }
    public DateTime? LockedUntil { get; set; }
}
