using System.ComponentModel.DataAnnotations;

namespace InventoryX.Domain.Models.Common
{
    /// <summary>
    /// Base for global (non-tenant-owned) entities: identity, audit stamps and
    /// a rowversion optimistic-concurrency token.
    /// </summary>
    public abstract class GlobalModel
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public DateTime CreatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }
        [Timestamp]
        public byte[]? RowVersion { get; set; }
    }

    /// <summary>
    /// Base for tenant-owned entities. TenantId is stamped by the SaveChanges
    /// interceptor and enforced by the EF global query filter.
    /// </summary>
    public abstract class BaseModel : GlobalModel
    {
        public Guid TenantId { get; set; }
    }
}
