using System.ComponentModel.DataAnnotations.Schema;
using InventoryX.Domain.Models.Common;

namespace InventoryX.Domain.Models.Tenancy
{
    /// <summary>Permission atoms combinable into role bundles (data-model Identity & Access).</summary>
    [Flags]
    public enum Permission : long
    {
        None = 0,
        Sell = 1 << 0,
        Refund = 1 << 1,
        Discount = 1 << 2,
        VoidSale = 1 << 3,
        ViewProfit = 1 << 4,
        ManageStock = 1 << 5,
        ManagePurchasing = 1 << 6,
        ManagePricing = 1 << 7,
        ManageUsers = 1 << 8,
        ViewReports = 1 << 9,
        ApproveAdjustments = 1 << 10,
    }

    /// <summary>
    /// Cycle 1 ships six fixed system roles (IsSystem, TenantId == Guid.Empty);
    /// custom roles (Cycle 3) will add tenant-owned rows, not schema.
    /// </summary>
    public class Role : GlobalModel
    {
        public required string Name { get; set; }
        public Permission Permissions { get; set; }
        /// <summary>Max discount percent grantable without escalation (FR-035); null = no cap.</summary>
        [Column(TypeName = "decimal(5,2)")]
        public decimal? MaxDiscountPercent { get; set; }
        /// <summary>Max refund value processable without authorization; null = unlimited.</summary>
        [Column(TypeName = "decimal(18,4)")]
        public decimal? MaxUnauthorizedRefundAmount { get; set; }
        public bool IsSystem { get; set; }
    }
}
