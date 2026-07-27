using InventoryX.Domain.Models.Common;

namespace InventoryX.Domain.Models.Catalog
{
    public enum ImportKind { Products, OpeningStock }

    public enum ImportJobStatus { Uploaded, Previewed, Committed, Abandoned }

    /// <summary>
    /// Two-step spreadsheet import (FR-018, research R12): upload → mapping →
    /// per-row preview → commit. Nothing persists to the catalogue before commit.
    /// </summary>
    public class ImportJob : BaseModel
    {
        public ImportKind Kind { get; set; }
        public required string FileName { get; set; }
        /// <summary>Raw uploaded file, kept for the life of the job.</summary>
        public byte[] FileContent { get; set; } = [];
        /// <summary>JSON detected header columns.</summary>
        public string DetectedColumns { get; set; } = "[]";
        /// <summary>JSON column → field mapping.</summary>
        public string? ColumnMapping { get; set; }
        /// <summary>JSON per-row parsed values and errors from the preview.</summary>
        public string? RowResults { get; set; }
        public ImportJobStatus Status { get; set; } = ImportJobStatus.Uploaded;
        public int CreatedCount { get; set; }
        public int UpdatedCount { get; set; }
        public int SkippedCount { get; set; }
    }
}
