namespace InventoryX.Application.Exceptions;

/// <summary>
/// Raised on optimistic-concurrency failures or illegal state transitions.
/// Surfaces as an RFC 7807 problem with status 409.
/// </summary>
public class ConflictException(string message) : Exception(message);
