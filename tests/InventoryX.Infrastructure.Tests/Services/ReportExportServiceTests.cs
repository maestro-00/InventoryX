using System.Text;
using ClosedXML.Excel;
using FluentAssertions;
using InventoryX.Infrastructure.Services;

namespace InventoryX.Infrastructure.Tests.Services;

public sealed class ReportExportServiceTests
{
    private static readonly IReadOnlyList<IReadOnlyDictionary<string, object?>> Rows =
    [
        new Dictionary<string, object?> { ["Date"] = "2026-08-04", ["Sales"] = 123.45m },
    ];

    [Theory]
    [InlineData("csv", "text/csv")]
    [InlineData("pdf", "application/pdf")]
    public Task Text_exports_have_expected_content_type_and_signature(string format, string contentType) => Verify(format, contentType);

    [Fact]
    public async Task Xlsx_export_is_a_readable_workbook_with_headers_and_values()
    {
        var document = await new ReportExportService().GenerateAsync("sales", "xlsx", Rows);
        using var stream = new MemoryStream(document.Content);
        using var workbook = new XLWorkbook(stream);

        document.ContentType.Should().Be("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        workbook.Worksheet(1).Cell(1, 1).GetString().Should().Be("Date");
        workbook.Worksheet(1).Cell(2, 2).GetDouble().Should().BeApproximately(123.45, 0.001);
    }

    private static async Task Verify(string format, string contentType)
    {
        var document = await new ReportExportService().GenerateAsync("sales", format, Rows);
        document.ContentType.Should().Be(contentType);
        var text = Encoding.UTF8.GetString(document.Content);
        if (format == "csv") text.Should().Contain("Date,Sales").And.Contain("123.45");
        else text.Should().StartWith("%PDF-1.4");
    }
}
