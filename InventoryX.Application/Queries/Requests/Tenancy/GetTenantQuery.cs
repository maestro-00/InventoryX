using MediatR;

namespace InventoryX.Application.Queries.Requests.Tenancy
{
    public class TenantDto
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string Country { get; init; } = "GH";
        public string Currency { get; init; } = "GHS";
        public string BusinessType { get; init; } = "Retail";
        public string ValuationMethod { get; init; } = "WeightedAverage";
        public string OnboardingChecklist { get; init; } = "{}";
        public bool SampleDataLoaded { get; init; }
        public decimal? AdjustmentApprovalThreshold { get; init; }
        public decimal? PoApprovalThreshold { get; init; }
        public decimal? TillVarianceThreshold { get; init; }
        public decimal? ReturnAuthorizationThreshold { get; init; }
        public bool RequireExpiryOnBatchReceipt { get; init; }
        public string? BillingEmail { get; init; }
        public string? Address { get; init; }
        public string? Phone { get; init; }
        public byte[]? RowVersion { get; init; }
    }

    public class GetTenantQuery : IRequest<TenantDto>;
}
