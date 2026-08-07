using InventoryX.Domain.Models.Common;

namespace InventoryX.Domain.Models.Auditing;

public enum ReportExportStatus { Pending, Completed, Failed }

public sealed class ReportExportJob : BaseModel
{
    public required string ReportType { get; set; }
    public required string Format { get; set; }
    public ReportExportStatus Status { get; set; } = ReportExportStatus.Pending;
    public string? FileName { get; set; }
    public string? ContentType { get; set; }
    public byte[]? Content { get; set; }
    public string? Error { get; set; }
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
}
