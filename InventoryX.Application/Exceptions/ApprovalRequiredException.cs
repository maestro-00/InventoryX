namespace InventoryX.Application.Exceptions;

/// <summary>
/// Raised when an operation is parked pending approval (adjustments/purchase
/// orders above threshold). Surfaces as an RFC 7807 problem with status 423.
/// </summary>
public class ApprovalRequiredException(string message, Guid? pendingEntityId = null) : Exception(message)
{
    public Guid? PendingEntityId { get; } = pendingEntityId;
}
