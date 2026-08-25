using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace InventoryX.Presentation.Swagger;

/// <summary>
/// Tags each operation from the controller route segment (e.g. sales, billing)
/// when no explicit [Tags] attribute is present.
/// </summary>
public sealed class ControllerTagsOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (operation.Tags is { Count: > 0 }) return;

        var relativePath = context.ApiDescription.RelativePath ?? string.Empty;
        var segments = relativePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        // Expect api/v1/{resource}/...
        var resource = segments.Length >= 3 ? segments[2] : (segments.LastOrDefault() ?? "api");
        var tag = char.ToUpperInvariant(resource[0]) + resource[1..];
        operation.Tags = [new OpenApiTag { Name = tag }];
    }
}
