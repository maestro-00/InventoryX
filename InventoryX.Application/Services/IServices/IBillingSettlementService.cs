namespace InventoryX.Application.Services.IServices;

/// <summary>Persists a provider-verified billing payment and its effects atomically.</summary>
public interface IBillingSettlementService
{
    Task<bool> SettleAsync(
        string reference, decimal verifiedAmount, string verifiedCurrency,
        DateTime confirmedAt, CancellationToken cancellationToken = default);
}
