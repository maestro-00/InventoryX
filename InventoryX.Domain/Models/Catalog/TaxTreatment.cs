using InventoryX.Domain.Models.Common;

namespace InventoryX.Domain.Models.Catalog
{
    /// <summary>
    /// Per-country tax configuration data (research R11), seeded for Ghana:
    /// GH-STD (VAT 15% + NHIL 2.5% + GETFund 2.5% + COVID HRL 1%), GH-ZERO,
    /// GH-EXEMPT. Components JSON carries rate lines and compounding rules.
    /// </summary>
    public class TaxTreatment : GlobalModel
    {
        public required string Code { get; set; }
        public required string Name { get; set; }
        /// <summary>ISO 3166 alpha-2 country the treatment belongs to.</summary>
        public string CountryCode { get; set; } = "GH";
        /// <summary>
        /// JSON array of components, e.g.
        /// [{"code":"NHIL","rate":0.025,"base":"net"},{"code":"VAT","rate":0.15,"base":"net+levies"}].
        /// </summary>
        public string ComponentsJson { get; set; } = "[]";
        public bool IsActive { get; set; } = true;
    }
}
