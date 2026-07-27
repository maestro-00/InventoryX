using System.Globalization;
using ClosedXML.Excel;
using InventoryX.Application.Services.IServices;

namespace InventoryX.Infrastructure.Services
{
    /// <summary>CSV/XLSX parsing for the two-step import (research R12).</summary>
    public class SpreadsheetImportService : ISpreadsheetImportService
    {
        private static readonly string[] ProductFields = ["name", "sku", "barcode", "category", "sellingPrice", "costPrice", "reorderPoint"];
        private static readonly string[] OpeningStockFields = ["sku", "barcode", "location", "qty", "unitCost"];

        public List<string> DetectColumns(byte[] fileContent, string fileName) =>
            ReadTable(fileContent, fileName).Headers;

        public List<ImportRowResult> ParseRows(
            byte[] fileContent, string fileName, Dictionary<string, string> columnMapping, string importKind)
        {
            var (headers, rows) = ReadTable(fileContent, fileName);
            var validFields = importKind.Equals("OpeningStock", StringComparison.OrdinalIgnoreCase)
                ? OpeningStockFields
                : ProductFields;

            var headerIndex = headers
                .Select((h, i) => (h, i))
                .ToDictionary(x => x.h, x => x.i, StringComparer.OrdinalIgnoreCase);

            var results = new List<ImportRowResult>();
            for (var r = 0; r < rows.Count; r++)
            {
                var values = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
                var errors = new List<string>();

                foreach (var (column, field) in columnMapping)
                {
                    var normalizedField = validFields.FirstOrDefault(
                        candidate => candidate.Equals(field, StringComparison.OrdinalIgnoreCase));
                    if (normalizedField is null)
                    {
                        errors.Add($"Unknown target field '{field}'.");
                        continue;
                    }
                    if (!headerIndex.TryGetValue(column, out var index))
                    {
                        errors.Add($"Column '{column}' not found in the file.");
                        continue;
                    }
                    values[normalizedField] = index < rows[r].Count ? rows[r][index]?.Trim() : null;
                }

                ValidateRow(values, errors, importKind);
                results.Add(new ImportRowResult(r + 2, values, errors)); // +2: 1-based + header row
            }

            return results;
        }

        private static void ValidateRow(Dictionary<string, string?> values, List<string> errors, string importKind)
        {
            if (importKind.Equals("OpeningStock", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(values.GetValueOrDefault("sku")) &&
                    string.IsNullOrWhiteSpace(values.GetValueOrDefault("barcode")))
                    errors.Add("A sku or barcode is required to match the product.");
                if (!decimal.TryParse(values.GetValueOrDefault("qty"), NumberStyles.Number, CultureInfo.InvariantCulture, out _))
                    errors.Add("qty must be a number.");
                var cost = values.GetValueOrDefault("unitCost");
                if (!string.IsNullOrWhiteSpace(cost) &&
                    !decimal.TryParse(cost, NumberStyles.Number, CultureInfo.InvariantCulture, out _))
                    errors.Add("unitCost must be a number.");
                return;
            }

            if (string.IsNullOrWhiteSpace(values.GetValueOrDefault("name")))
                errors.Add("name is required.");
            var price = values.GetValueOrDefault("sellingPrice");
            if (!string.IsNullOrWhiteSpace(price) &&
                !decimal.TryParse(price, NumberStyles.Number, CultureInfo.InvariantCulture, out _))
                errors.Add("sellingPrice must be a number.");
            var costPrice = values.GetValueOrDefault("costPrice");
            if (!string.IsNullOrWhiteSpace(costPrice) &&
                !decimal.TryParse(costPrice, NumberStyles.Number, CultureInfo.InvariantCulture, out _))
                errors.Add("costPrice must be a number.");
        }

        private static (List<string> Headers, List<List<string?>> Rows) ReadTable(byte[] fileContent, string fileName)
        {
            return fileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase)
                ? ReadCsv(fileContent)
                : ReadXlsx(fileContent);
        }

        private static (List<string>, List<List<string?>>) ReadXlsx(byte[] fileContent)
        {
            using var stream = new MemoryStream(fileContent);
            using var workbook = new XLWorkbook(stream);
            var sheet = workbook.Worksheets.First();
            var used = sheet.RangeUsed();
            if (used is null) return ([], []);

            var headerRow = used.FirstRow();
            var headers = headerRow.Cells().Select(c => c.GetString().Trim()).ToList();
            var rows = new List<List<string?>>();
            foreach (var row in used.Rows().Skip(1))
            {
                if (row.Cells().All(c => c.IsEmpty())) continue;
                rows.Add(Enumerable.Range(1, headers.Count)
                    .Select(i => (string?)row.Cell(i).GetString().Trim())
                    .ToList());
            }
            return (headers, rows);
        }

        private static (List<string>, List<List<string?>>) ReadCsv(byte[] fileContent)
        {
            using var reader = new StreamReader(new MemoryStream(fileContent));
            var lines = new List<string>();
            while (reader.ReadLine() is { } line)
            {
                if (!string.IsNullOrWhiteSpace(line)) lines.Add(line);
            }
            if (lines.Count == 0) return ([], []);

            var headers = SplitCsvLine(lines[0]).Select(h => h.Trim()).ToList();
            var rows = lines.Skip(1)
                .Select(l => SplitCsvLine(l).Select(v => (string?)v.Trim()).ToList())
                .ToList();
            return (headers, rows);
        }

        private static List<string> SplitCsvLine(string line)
        {
            var result = new List<string>();
            var current = new System.Text.StringBuilder();
            var inQuotes = false;
            for (var i = 0; i < line.Length; i++)
            {
                var ch = line[i];
                if (ch == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"') { current.Append('"'); i++; }
                    else inQuotes = !inQuotes;
                }
                else if (ch == ',' && !inQuotes)
                {
                    result.Add(current.ToString());
                    current.Clear();
                }
                else current.Append(ch);
            }
            result.Add(current.ToString());
            return result;
        }
    }
}
