namespace InventoryX.Application.Behaviors
{
    /// <summary>
    /// Marker for sensitive commands (price change, refund, void, adjustment,
    /// permission change — FR-008). AuditBehavior writes an AuditLogEntry for
    /// every executed command implementing this.
    /// </summary>
    public interface IAuditedCommand
    {
        /// <summary>Short action name recorded in the audit trail, e.g. "sale.void".</summary>
        string AuditAction { get; }
        /// <summary>Type name of the entity the action targets.</summary>
        string AuditEntityType { get; }
        /// <summary>Identifier of the targeted entity (may be resolved after handling).</summary>
        string AuditEntityId { get; }
    }
}
