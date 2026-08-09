using InventoryX.Domain.Models.Common;

namespace InventoryX.Domain.Models.Auditing;

public enum ReportCadence { Daily, Weekly, Monthly }

public sealed class ReportSchedule : BaseModel
{
    public required string ReportType { get; set; }
    public ReportCadence Cadence { get; set; }
    public required string Format { get; set; }
    public required string RecipientsJson { get; set; }
    public Guid? LocationId { get; set; }
    public Guid? CategoryId { get; set; }
    public string? StaffId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime NextRunAt { get; set; }
    public DateTime? LastRunAt { get; set; }
}
