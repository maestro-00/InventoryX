using System.Text.Json;
using FluentAssertions;
using InventoryX.Application.DTOs.Common;
using InventoryX.Presentation.Tests.Scenarios;

namespace InventoryX.Presentation.Tests.Swagger;

public sealed class ContractSurfaceTests
{
    [Fact]
    public async Task Swagger_matches_cycle_one_contract_surface_and_openapi_conventions()
    {
        await using var factory = new TestAppFactory();
        using var client = factory.CreateClient();
        using var document = JsonDocument.Parse(await client.GetStringAsync("/swagger/v1/swagger.json"));
        var root = document.RootElement;
        var paths = root.GetProperty("paths");

        foreach (var (method, path) in Expected)
        {
            Assert.True(paths.TryGetProperty("/api/v1" + path, out var operations), $"Missing path {path}");
            Assert.True(operations.TryGetProperty(method, out _), $"Missing {method.ToUpperInvariant()} {path}");
        }

        foreach (var path in paths.EnumerateObject())
            path.Name.Should().NotStartWith("/api/auth", "Identity /api/auth must be excluded from OpenAPI");

        var info = root.GetProperty("info");
        info.GetProperty("title").GetString().Should().Be("InventoryX API");
        info.GetProperty("version").GetString().Should().Be("v1");

        var schemes = root.GetProperty("components").GetProperty("securitySchemes");
        schemes.TryGetProperty("oauth2", out _).Should().BeFalse();
        var bearer = schemes.GetProperty("Bearer");
        bearer.GetProperty("type").GetString().Should().Be("http");
        bearer.GetProperty("scheme").GetString().Should().Be("bearer");

        AssertPagedList(paths, "/api/v1/users");
        AssertPagedList(paths, "/api/v1/suppliers");
        AssertPagedList(paths, "/api/v1/billing/invoices");
        AssertPagedList(paths, "/api/v1/transfers");

        var listSchedules = paths.GetProperty("/api/v1/reports/schedules").GetProperty("get");
        var parameters = listSchedules.GetProperty("parameters").EnumerateArray().ToList();
        var page = parameters.Single(parameter =>
            string.Equals(parameter.GetProperty("name").GetString(), "page", StringComparison.OrdinalIgnoreCase));
        var pageSize = parameters.Single(parameter =>
            string.Equals(parameter.GetProperty("name").GetString(), "pageSize", StringComparison.OrdinalIgnoreCase));
        page.GetProperty("schema").GetProperty("default").GetInt32().Should().Be(1);
        pageSize.GetProperty("schema").GetProperty("default").GetInt32().Should().Be(PageRequest.DefaultPageSize);
        pageSize.GetProperty("schema").GetProperty("maximum").GetInt32().Should().Be(PageRequest.MaxPageSize);
        var responseSchema = listSchedules.GetProperty("responses").GetProperty("200")
            .GetProperty("content").EnumerateObject().First().Value.GetProperty("schema")
            .GetProperty("$ref").GetString();
        responseSchema.Should().Contain("PagedResult").And.Contain("ReportScheduleDto");

        var createSale = paths.GetProperty("/api/v1/sales").GetProperty("post");
        var saleBodyRef = createSale.GetProperty("requestBody").GetProperty("content")
            .EnumerateObject().First().Value.GetProperty("schema").GetProperty("$ref").GetString()!;
        var saleSchemaName = saleBodyRef.Split('/').Last();
        var saleSchema = root.GetProperty("components").GetProperty("schemas").GetProperty(saleSchemaName);
        var saleProps = saleSchema.GetProperty("properties").EnumerateObject().Select(p => p.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        saleProps.Should().NotContain("allowNegativeStock");
        saleProps.Should().NotContain("acceptHistoricalFiscalSnapshot");

        var patchProduct = paths.GetProperty("/api/v1/products/{id}").GetProperty("patch");
        patchProduct.GetProperty("responses").TryGetProperty("409", out _).Should().BeTrue();

        var createPo = paths.GetProperty("/api/v1/purchase-orders").GetProperty("post");
        createPo.GetProperty("responses").TryGetProperty("402", out _).Should().BeTrue();

        var availability = paths.GetProperty("/api/v1/products/{id}/availability").GetProperty("get");
        availability.TryGetProperty("x-inventoryx-live-only", out var liveOnly).Should().BeTrue();
        liveOnly.GetBoolean().Should().BeTrue();
    }

    private static void AssertPagedList(JsonElement paths, string path)
    {
        var get = paths.GetProperty(path).GetProperty("get");
        var parameters = get.TryGetProperty("parameters", out var parms)
            ? parms.EnumerateArray().ToList()
            : [];
        parameters.Should().Contain(p => string.Equals(p.GetProperty("name").GetString(), "page", StringComparison.OrdinalIgnoreCase));
        parameters.Should().Contain(p => string.Equals(p.GetProperty("name").GetString(), "pageSize", StringComparison.OrdinalIgnoreCase));
        var schemaRef = get.GetProperty("responses").GetProperty("200")
            .GetProperty("content").EnumerateObject().First().Value.GetProperty("schema")
            .GetProperty("$ref").GetString();
        schemaRef.Should().Contain("PagedResult");
    }

    private static readonly (string Method, string Path)[] Expected =
    [
        ("post", "/auth/register"), ("post", "/auth/login"), ("post", "/auth/google"),
        ("post", "/auth/refresh"), ("post", "/auth/logout"),
        ("get", "/tenant"), ("patch", "/tenant"), ("post", "/tenant/export"), ("get", "/tenant/export/{jobId}"),
        ("get", "/users"), ("post", "/users/invitations"), ("post", "/users/invitations/{id}/accept"), ("patch", "/users/{id}"),
        ("get", "/roles"), ("get", "/audit-log"), ("get", "/billing/plans"), ("get", "/billing/subscription"),
        ("get", "/categories"), ("post", "/categories"), ("get", "/products"), ("get", "/products/{id}"),
        ("get", "/tax-treatments"), ("post", "/import/products"), ("get", "/export/products"),
        ("get", "/locations"), ("get", "/stock"), ("get", "/stock/movements"), ("get", "/products/{id}/batches"),
        ("get", "/batches/{id}/trace"), ("post", "/transfers"), ("get", "/transfers"), ("post", "/counts"), ("get", "/alerts"),
        ("get", "/registers"), ("patch", "/registers/{id}"), ("get", "/registers/{registerId}/shifts"), ("post", "/registers/{registerId}/shifts"),
        ("get", "/shifts"), ("post", "/shifts/{shiftId}/close"),
        ("post", "/sales"), ("post", "/returns"), ("get", "/sync/snapshot"), ("post", "/sync/sales"),
        ("get", "/sync/rejected"), ("post", "/sync/rejected/{rejectedSaleId}/resolve"),
        ("get", "/suppliers"), ("patch", "/suppliers/{id}"), ("get", "/suppliers/{id}/products"), ("get", "/suppliers/{id}/orders"),
        ("get", "/purchase-orders"), ("post", "/purchase-orders"), ("post", "/supplier-invoices"),
        ("get", "/dashboard"), ("get", "/reports/sales"), ("get", "/reports/schedules"), ("post", "/reports/schedules"),
        ("get", "/notifications"), ("get", "/notification-preferences"),
        ("get", "/billing/invoices"),
        ("delete", "/locations/{id}"),
        ("get", "/export/stock"),
        ("post", "/stock/movements/{id}/correct"),
        ("get", "/suppliers/{id}/performance"),
        ("get", "/purchase-orders/{id}/pdf"),
        ("get", "/reports/{reportType}/export"),
        ("get", "/reports/export-jobs/{id}"),
        ("get", "/sales/held/{id}"),
        ("put", "/tenant/receipt-template"),
    ];
}
