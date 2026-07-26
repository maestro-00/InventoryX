using InventoryX.Domain.Models.Common;

namespace InventoryX.Domain.Models.Auditing
{
    /// <summary>
    /// Append-only audit trail for sensitive commands (FR-008). Never updated
    /// or deleted; corrections are new entries.
    /// </summary>
    public class AuditLogEntry : BaseModel
    {
        public required string Actor { get; set; }
        public required string Action { get; set; }
        public required string EntityType { get; set; }
        public required string EntityId { get; set; }
        public string? BeforeJson { get; set; }
        public string? AfterJson { get; set; }
        public DateTime OccurredAt { get; set; }
        public string? Ip { get; set; }
    }
}
