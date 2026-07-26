using Microsoft.AspNetCore.Identity;

namespace InventoryX.Domain.Models
{
    public enum UserStatus { Invited, Active, Deactivated }

    public class User : IdentityUser
    {
        public string? Name { get; set; }
        /// <summary>Owning tenant; null only for platform-level accounts mid-registration.</summary>
        public Guid? TenantId { get; set; }
        /// <summary>Exactly one irremovable owner per tenant (FR-003).</summary>
        public bool IsOwner { get; set; }
        /// <summary>Assigned role bundle (system role in Cycle 1).</summary>
        public Guid? RoleId { get; set; }
        /// <summary>Comma-separated location ids a Manager is scoped to; "*" = all (FR-004).</summary>
        public string? LocationScope { get; set; }
        public UserStatus Status { get; set; } = UserStatus.Active;
    }
}
