namespace InventoryX.Application.Services.IServices;

public sealed record ReportExportDocument(string FileName, string ContentType, byte[] Content);

public interface IReportExportService
{
    Task<ReportExportDocument> GenerateAsync(string reportType, string format,
        IReadOnlyList<IReadOnlyDictionary<string, object?>> rows, CancellationToken cancellationToken = default);
}
