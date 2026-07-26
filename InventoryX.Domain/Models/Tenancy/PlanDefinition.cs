using System.ComponentModel.DataAnnotations.Schema;
using InventoryX.Domain.Models.Common;

namespace InventoryX.Domain.Models.Tenancy
{
    public enum PlanTier { Free, Standard, Professional, Enterprise }

    /// <summary>Global (not tenant-owned) subscription plan catalogue, seeded from configuration.</summary>
    public class PlanDefinition : GlobalModel
    {
        public PlanTier Tier { get; set; }
        public required string Name { get; set; }
        [Column(TypeName = "decimal(18,4)")]
        public decimal MonthlyPrice { get; set; }
        [Column(TypeName = "decimal(18,4)")]
        public decimal AnnualPrice { get; set; }

        // Limits — null means unlimited
        public int? MaxLocations { get; set; }
        public int? MaxUsers { get; set; }
        public int? MaxProducts { get; set; }
        public int? MaxRegisters { get; set; }
        public int? MonthlySaleCap { get; set; }
        public int? HistoryMonths { get; set; }

        // Feature gates
        public bool PurchaseOrders { get; set; }
        public bool BatchExpiry { get; set; }
        public bool Serials { get; set; }
        public bool MultiCurrency { get; set; }
        public bool CustomRoles { get; set; }
        public bool AdvancedReports { get; set; }
        public bool Integrations { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
