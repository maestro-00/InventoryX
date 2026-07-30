using System.ComponentModel.DataAnnotations.Schema;
using InventoryX.Domain.Models.Common;

namespace InventoryX.Domain.Models.Tenancy
{
    public enum BusinessType { Retail, Food, Pharmacy, Wholesale, Service, Other }

    /// <summary>WeightedAverage only in Cycle 1; Fifo/SpecificCost reserved.</summary>
    public enum ValuationMethod { WeightedAverage, Fifo, SpecificCost }

    /// <summary>
    /// The business account and isolation root. Not itself tenant-filtered.
    /// </summary>
    public class Tenant : GlobalModel
    {
        public required string Name { get; set; }
        /// <summary>ISO 3166 alpha-2, e.g. GH.</summary>
        public string Country { get; set; } = "GH";
        /// <summary>ISO 4217, e.g. GHS.</summary>
        public string Currency { get; set; } = "GHS";
        public BusinessType BusinessType { get; set; } = BusinessType.Retail;
        public ValuationMethod ValuationMethod { get; set; } = ValuationMethod.WeightedAverage;

        /// <summary>JSON step flags driving the guided onboarding checklist.</summary>
        public string OnboardingChecklist { get; set; } = "{}";
        public bool SampleDataLoaded { get; set; }

        /// <summary>Adjustments above this value require approval (null = never).</summary>
        [Column(TypeName = "decimal(18,4)")]
        public decimal? AdjustmentApprovalThreshold { get; set; }
        /// <summary>Purchase orders above this value require approval (null = never).</summary>
        [Column(TypeName = "decimal(18,4)")]
        public decimal? PoApprovalThreshold { get; set; }
        /// <summary>Absolute till variance that flags a closed shift for review.</summary>
        [Column(TypeName = "decimal(18,4)")]
        public decimal? TillVarianceThreshold { get; set; }
        /// <summary>Refunds above this value (or receiptless returns) require authorization.</summary>
        [Column(TypeName = "decimal(18,4)")]
        public decimal? ReturnAuthorizationThreshold { get; set; }

        /// <summary>Batch-tracked receipts must capture expiry (default on for Food/Pharmacy).</summary>
        public bool RequireExpiryOnBatchReceipt { get; set; }

        /// <summary>JSON receipt template customization (header, footer, fields).</summary>
        public string? ReceiptTemplate { get; set; }
        public string? BillingEmail { get; set; }
        public string? BillingTaxNumber { get; set; }
        public string? Address { get; set; }
        public string? Phone { get; set; }
    }
}
