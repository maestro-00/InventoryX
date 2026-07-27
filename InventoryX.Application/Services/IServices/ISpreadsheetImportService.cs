namespace InventoryX.Application.Services.IServices
{
    public record ImportRowResult(int RowNumber, Dictionary<string, string?> Values, List<string> Errors)
    {
        public bool IsValid => Errors.Count == 0;
    }

    /// <summary>
    /// Parses CSV/XLSX uploads for the two-step import flow (FR-018, research
    /// R12): detect columns on upload, build a full per-row preview once a
    /// mapping is set, nothing persisted until commit.
    /// </summary>
    public interface ISpreadsheetImportService
    {
        /// <summary>Header columns detected in the file.</summary>
        List<string> DetectColumns(byte[] fileContent, string fileName);

        /// <summary>Parses every data row through the column→field mapping, collecting per-row errors.</summary>
        List<ImportRowResult> ParseRows(byte[] fileContent, string fileName, Dictionary<string, string> columnMapping, string importKind);
    }
}
