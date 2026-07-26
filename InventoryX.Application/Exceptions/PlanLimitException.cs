namespace InventoryX.Application.Exceptions;

/// <summary>
/// Raised when an operation would exceed the tenant's subscription plan limits.
/// Surfaces as an RFC 7807 problem with status 402 and an upgrade hint.
/// </summary>
public class PlanLimitException(string message, string? upgradeHint = null) : Exception(message)
{
    public string? UpgradeHint { get; } = upgradeHint;
}
