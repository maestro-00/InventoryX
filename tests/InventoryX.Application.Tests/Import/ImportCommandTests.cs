using System.Text;
using FluentAssertions;
using InventoryX.Application.Commands.RequestHandlers.Import;
using InventoryX.Application.Commands.Requests.Import;
using InventoryX.Common.Tests;
using InventoryX.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace InventoryX.Application.Tests.Import;

/// <summary>T040 - imports preview every row without touching the catalogue before commit.</summary>
public sealed class ImportCommandTests : IDisposable
{
    private readonly TestDb _db = new(Guid.NewGuid());

    [Fact]
    public async Task Product_preview_persists_nothing_until_commit()
    {
        await using var context = _db.CreateContext();
        var parser = new SpreadsheetImportService();
        var upload = new CreateImportJobCommandHandler(context, parser);
        var mapping = new SetImportMappingCommandHandler(context, parser);

        var job = await upload.Handle(new CreateImportJobCommand
        {
            Kind = "Products",
            FileName = "products.csv",
            FileContent = Encoding.UTF8.GetBytes("Product Name,SKU,Price\nSugar,SUG-001,10.00"),
        }, CancellationToken.None);

        var preview = await mapping.Handle(new SetImportMappingCommand
        {
            JobId = job.Id,
            ColumnMapping = new Dictionary<string, string>
            {
                ["Product Name"] = "Name",
                ["SKU"] = "SKU",
                ["Price"] = "SellingPrice",
            },
        }, CancellationToken.None);

        preview.Preview.Should().ContainSingle(row => row.IsValid);
        (await context.Products.CountAsync()).Should().Be(0);

        var commit = new CommitImportCommandHandler(context, new StockLedger(context));
        var result = await commit.Handle(
            new CommitImportCommand { JobId = job.Id },
            CancellationToken.None);

        result.CreatedCount.Should().Be(1);
        (await context.Products.SingleAsync()).Sku.Should().Be("SUG-001");
    }

    public void Dispose() => _db.Dispose();
}
