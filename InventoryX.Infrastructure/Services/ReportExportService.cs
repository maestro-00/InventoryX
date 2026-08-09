using System.Globalization;
using System.Text;
using ClosedXML.Excel;
using InventoryX.Application.Services.IServices;

namespace InventoryX.Infrastructure.Services;

public sealed class ReportExportService : IReportExportService
{
    public Task<ReportExportDocument> GenerateAsync(string reportType, string format,
        IReadOnlyList<IReadOnlyDictionary<string, object?>> rows, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        format = format.Trim().ToLowerInvariant();
        var document = format switch
        {
            "csv" => new ReportExportDocument($"{reportType}.csv", "text/csv", CreateCsv(rows)),
            "xlsx" => new ReportExportDocument($"{reportType}.xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", CreateXlsx(reportType, rows)),
            "pdf" => new ReportExportDocument($"{reportType}.pdf", "application/pdf", CreatePdf(reportType, rows)),
            _ => throw new FluentValidation.ValidationException("Export format must be csv, xlsx, or pdf."),
        };
        return Task.FromResult(document);
    }

    private static byte[] CreateCsv(IReadOnlyList<IReadOnlyDictionary<string, object?>> rows)
    {
        var headers = Headers(rows);
        var output = new StringBuilder().AppendLine(string.Join(',', headers.Select(EscapeCsv)));
        foreach (var row in rows)
            output.AppendLine(string.Join(',', headers.Select(header => EscapeCsv(Format(row.GetValueOrDefault(header))))));
        return Encoding.UTF8.GetBytes(output.ToString());
    }

    private static byte[] CreateXlsx(string reportType, IReadOnlyList<IReadOnlyDictionary<string, object?>> rows)
    {
        var headers = Headers(rows);
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add(SafeSheetName(reportType));
        for (var column = 0; column < headers.Count; column++) sheet.Cell(1, column + 1).Value = headers[column];
        for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++)
        for (var column = 0; column < headers.Count; column++)
        {
            var value = rows[rowIndex].GetValueOrDefault(headers[column]);
            sheet.Cell(rowIndex + 2, column + 1).Value = value switch
            {
                null => Blank.Value,
                decimal number => number,
                double number => number,
                int number => number,
                long number => number,
                DateTime date => date,
                bool flag => flag,
                _ => Format(value),
            };
        }
        sheet.Row(1).Style.Font.Bold = true;
        sheet.Columns().AdjustToContents();
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static byte[] CreatePdf(string reportType, IReadOnlyList<IReadOnlyDictionary<string, object?>> rows)
    {
        var headers = Headers(rows);
        var lines = new List<string> { reportType.ToUpperInvariant(), string.Join(" | ", headers) };
        lines.AddRange(rows.Take(45).Select(row => string.Join(" | ", headers.Select(header => Format(row.GetValueOrDefault(header))))));
        var commands = new StringBuilder("BT /F1 8 Tf 36 760 Td 11 TL\n");
        foreach (var line in lines) commands.Append('(').Append(EscapePdf(line)).Append(") Tj T*\n");
        commands.Append("ET");
        var stream = commands.ToString();
        return Encoding.UTF8.GetBytes($"%PDF-1.4\n1 0 obj<</Type/Catalog/Pages 2 0 R>>endobj\n2 0 obj<</Type/Pages/Count 1/Kids[3 0 R]>>endobj\n3 0 obj<</Type/Page/Parent 2 0 R/MediaBox[0 0 612 792]/Resources<</Font<</F1 4 0 R>>>>/Contents 5 0 R>>endobj\n4 0 obj<</Type/Font/Subtype/Type1/BaseFont/Helvetica>>endobj\n5 0 obj<</Length {Encoding.UTF8.GetByteCount(stream)}>>stream\n{stream}\nendstream\nendobj\ntrailer<</Root 1 0 R>>\n%%EOF");
    }

    private static List<string> Headers(IReadOnlyList<IReadOnlyDictionary<string, object?>> rows) =>
        rows.SelectMany(row => row.Keys).Distinct(StringComparer.Ordinal).ToList();
    private static string Format(object? value) => value switch
    { null => string.Empty, DateTime date => date.ToString("O", CultureInfo.InvariantCulture), IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture), _ => value.ToString() ?? string.Empty };
    private static string EscapeCsv(string value) => value.IndexOfAny([',', '"', '\r', '\n']) >= 0 ? $"\"{value.Replace("\"", "\"\"")}\"" : value;
    private static string EscapePdf(string value) => value.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
    private static string SafeSheetName(string value)
    {
        var name = new string(value.Where(character => !"[]:*?/\\".Contains(character)).Take(31).ToArray());
        return name.Length > 0 ? name : "Report";
    }
}
