namespace InventoryX.Application.Exceptions;

public sealed class BillingIdempotencyConflictException(string message) : ConflictException(message);
