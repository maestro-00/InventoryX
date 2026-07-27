using MediatR;

namespace InventoryX.Application.Commands.Requests.Tenancy
{
    public record RegisterTenantResult(
        Guid TenantId,
        string BusinessName,
        string SubscriptionStatus,
        string AccessToken,
        DateTime AccessTokenExpiresAt,
        string RefreshToken);

    /// <summary>Creates tenant + owner + Trialing Professional subscription (FR-001, FR-011).</summary>
    public class RegisterTenantCommand : IRequest<RegisterTenantResult>
    {
        public required string Email { get; init; }
        public required string Password { get; init; }
        public required string BusinessName { get; init; }
        public string Country { get; init; } = "GH";
        public string Currency { get; init; } = "GHS";
        public string BusinessType { get; init; } = "Retail";
    }
}
