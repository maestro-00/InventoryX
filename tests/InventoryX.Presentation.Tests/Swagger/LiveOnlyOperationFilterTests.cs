using FluentAssertions;
using InventoryX.Presentation.Controllers.v1;
using InventoryX.Presentation.Swagger;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Moq;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace InventoryX.Presentation.Tests.Swagger;

public sealed class LiveOnlyOperationFilterTests
{
    [Fact]
    public void Availability_operation_emits_live_only_extensions()
    {
        var method = typeof(ProductsController).GetMethod(nameof(ProductsController.Availability))!;
        var operation = new OpenApiOperation();
        var context = new OperationFilterContext(
            new ApiDescription(), Mock.Of<ISchemaGenerator>(), new SchemaRepository(), method);

        new LiveOnlyOperationFilter().Apply(operation, context);

        ((OpenApiBoolean)operation.Extensions["x-inventoryx-live-only"]).Value.Should().BeTrue();
        operation.Extensions.Should().ContainKey("x-inventoryx-live-only-reason");
    }
}
