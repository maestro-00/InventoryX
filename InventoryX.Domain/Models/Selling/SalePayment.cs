using System.ComponentModel.DataAnnotations.Schema;
using InventoryX.Domain.Models.Common;

namespace InventoryX.Domain.Models.Selling
{
    /// <summary>
    /// Recorded tenders in Cycle 1; StoreCredit/GiftCard/LoyaltyPoints/OnAccount
    /// are reserved enum values deferred with their balance ledgers.
    /// </summary>
    public enum TenderType { Cash, Card, MobileMoney, BankTransfer, Cheque, StoreCredit, GiftCard, LoyaltyPoints, OnAccount }

    /// <summary>Split tender = multiple rows per sale (FR-039).</summary>
    public class SalePayment : BaseModel
    {
        public Guid SaleId { get; set; }
        public TenderType Tender { get; set; }
        [Column(TypeName = "decimal(18,4)")]
        public decimal Amount { get; set; }
        /// <summary>External reference (MoMo transaction id, card slip no., cheque no.).</summary>
        public string? Reference { get; set; }
    }
}
