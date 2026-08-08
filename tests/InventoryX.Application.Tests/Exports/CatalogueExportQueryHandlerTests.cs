using System.Text;
using InventoryX.Application.Queries.RequestHandlers.Exports;
using InventoryX.Application.Queries.Requests.Exports;
using InventoryX.Common.Tests;
using InventoryX.Domain.Models.Catalog;
using InventoryX.Domain.Models.Inventory;
using InventoryX.Infrastructure.Services;

namespace InventoryX.Application.Tests.Exports;

public sealed class CatalogueExportQueryHandlerTests
{
    [Fact]
    public async Task Product_export_redacts_cost_without_profit_permission()
    {
        var tenantId = Guid.NewGuid();
        using var database = new TestDb(tenantId);
        await using var context = database.CreateContext();
        context.Products.Add(new Product { Name = "Coffee", SellingPrice = 15m, CostPrice = 7m });
        await context.SaveChangesAsync();

        var handler = new ExportCatalogueQueryHandler(context, new ReportExportService());
        var document = await handler.Handle(new ExportCatalogueQuery("products", "csv", false), default);
        var csv = Encoding.UTF8.GetString(document.Content);

        Assert.Contains("Coffee", csv);
        Assert.DoesNotContain(",7,", csv);
    }

    [Fact]
    public async Task Stock_export_contains_product_location_and_quantities()
    {
        var tenantId = Guid.NewGuid();
        using var database = new TestDb(tenantId);
        await using var context = database.CreateContext();
        var product = new Product { Name = "Tea", SellingPrice = 10m, CostPrice = 4m };
        var location = new Location { Name = "Main Shop" };
        context.AddRange(product, location);
        await context.SaveChangesAsync();
        context.StockLevels.Add(new StockLevel { ProductId = product.Id, LocationId = location.Id, QtyOnHand = 12m });
        await context.SaveChangesAsync();

        var handler = new ExportCatalogueQueryHandler(context, new ReportExportService());
        var document = await handler.Handle(new ExportCatalogueQuery("stock", "csv", true), default);
        var csv = Encoding.UTF8.GetString(document.Content);

        Assert.Contains("Tea", csv);
        Assert.Contains("Main Shop", csv);
        Assert.Contains("12", csv);
    }
}
