using System.Text.Json;
using FluentAssertions;
using InventoryX.Application.DTOs.Common;
using InventoryX.Presentation.Tests.Scenarios;

namespace InventoryX.Presentation.Tests.Swagger;

public sealed class ContractSurfaceTests
{
    [Fact]
    public async Task Swagger_contains_cycle_one_contract_surface()
    {
        await using var factory = new TestAppFactory();
        using var client = factory.CreateClient();
        using var document = JsonDocument.Parse(await client.GetStringAsync("/swagger/v1/swagger.json"));
        var paths = document.RootElement.GetProperty("paths");
        foreach (var (method, path) in Expected)
        {
            Assert.True(paths.TryGetProperty("/api/v1" + path, out var operations), $"Missing path {path}");
            Assert.True(operations.TryGetProperty(method, out _), $"Missing {method.ToUpperInvariant()} {path}");
        }

        var listSchedules = paths.GetProperty("/api/v1/reports/schedules").GetProperty("get");
        var parameters = listSchedules.GetProperty("parameters").EnumerateArray().ToList();
        var page = parameters.Single(parameter => parameter.GetProperty("name").GetString() == "page");
        var pageSize = parameters.Single(parameter => parameter.GetProperty("name").GetString() == "pageSize");
        page.GetProperty("schema").GetProperty("default").GetInt32().Should().Be(1);
        pageSize.GetProperty("schema").GetProperty("default").GetInt32().Should().Be(PageRequest.DefaultPageSize);
        pageSize.GetProperty("schema").GetProperty("maximum").GetInt32().Should().Be(PageRequest.MaxPageSize);
        var responseSchema = listSchedules.GetProperty("responses").GetProperty("200")
            .GetProperty("content").EnumerateObject().First().Value.GetProperty("schema")
            .GetProperty("$ref").GetString();
        responseSchema.Should().Contain("PagedResult").And.Contain("ReportScheduleDto");
    }

    private static readonly (string Method, string Path)[] Expected =
    [
        ("post", "/auth/register"), ("post", "/auth/login"), ("post", "/auth/google"),
        ("get", "/tenant"), ("patch", "/tenant"), ("post", "/tenant/export"), ("get", "/tenant/export/{jobId}"),
        ("get", "/users"), ("post", "/users/invitations"), ("post", "/users/invitations/{id}/accept"), ("patch", "/users/{id}"),
        ("get", "/roles"), ("get", "/audit-log"), ("get", "/billing/plans"), ("get", "/billing/subscription"),
        ("get", "/categories"), ("post", "/categories"), ("get", "/products"), ("get", "/products/{id}"),
        ("get", "/tax-treatments"), ("post", "/import/products"), ("get", "/export/products"),
        ("get", "/locations"), ("get", "/stock"), ("get", "/stock/movements"), ("get", "/products/{id}/batches"),
        ("get", "/batches/{id}/trace"), ("post", "/transfers"), ("post", "/counts"), ("get", "/alerts"),
        ("get", "/registers"), ("post", "/registers/{registerId}/shifts"), ("post", "/shifts/{shiftId}/close"),
        ("post", "/sales"), ("post", "/returns"), ("get", "/sync/snapshot"), ("post", "/sync/sales"),
        ("get", "/suppliers"), ("get", "/suppliers/{id}/products"), ("get", "/suppliers/{id}/orders"),
        ("get", "/purchase-orders"), ("post", "/purchase-orders"), ("post", "/supplier-invoices"),
        ("get", "/dashboard"), ("get", "/reports/sales"), ("get", "/reports/schedules"), ("post", "/reports/schedules"),
        ("get", "/notifications"), ("get", "/notification-preferences"),
    ];
}
