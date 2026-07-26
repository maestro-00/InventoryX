using InventoryX.Domain.Models.Common;

namespace InventoryX.Domain.Models.Tenancy
{
    public enum UsageMetric { SalesThisMonth, Products, Users, Locations, Registers }

    /// <summary>
    /// Transactionally-maintained usage tallies consulted by the plan enforcer
    /// (FR-010). PeriodKey is e.g. "2026-07" for monthly metrics, "*" for
    /// lifetime metrics.
    /// </summary>
    public class UsageCounter : BaseModel
    {
        public UsageMetric Metric { get; set; }
        public string PeriodKey { get; set; } = "*";
        public int Count { get; set; }
    }
}
