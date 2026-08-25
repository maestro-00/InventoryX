using InventoryX.Application.Behaviors;
using InventoryX.Application.Queries.Requests.Tenancy;
using MediatR;

namespace InventoryX.Application.Commands.Requests.Tenancy
{
    /// <summary>Profile + threshold updates; valuation change needs an explicit confirmation flag (FR-028).</summary>
    public class UpdateTenantCommand : IRequest<TenantDto>, ITenantWriteCommand, IAuditedCommand
    {
        public string? Name { get; init; }
        public string? Address { get; init; }
        public string? Phone { get; init; }
        public string? BillingEmail { get; init; }
        public string? ValuationMethod { get; init; }
        public bool ConfirmValuationChange { get; init; }
        public decimal? AdjustmentApprovalThreshold { get; init; }
        public decimal? PoApprovalThreshold { get; init; }
        public decimal? TillVarianceThreshold { get; init; }
        public decimal? ReturnAuthorizationThreshold { get; init; }
        public bool? RequireExpiryOnBatchReceipt { get; init; }
        public string? OnboardingChecklist { get; init; }
        public byte[]? ExpectedRowVersion { get; init; }

        public string AuditAction => "tenant.update";
        public string AuditEntityType => "Tenant";
        public string AuditEntityId => "self";
    }

    public class LoadSampleDataCommand : IRequest<bool>, ITenantWriteCommand;

    public class RemoveSampleDataCommand : IRequest<bool>, ITenantWriteCommand;
}
