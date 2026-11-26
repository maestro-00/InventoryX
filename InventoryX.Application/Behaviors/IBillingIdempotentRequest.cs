namespace InventoryX.Application.Behaviors;

public interface IBillingIdempotentRequest
{
    string? IdempotencyKey { get; }
}
