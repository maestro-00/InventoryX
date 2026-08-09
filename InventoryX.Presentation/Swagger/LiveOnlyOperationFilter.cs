using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace InventoryX.Presentation.Swagger;

public sealed class LiveOnlyOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var marker = context.MethodInfo.GetCustomAttributes(true).OfType<LiveOnlyAttribute>().FirstOrDefault()
                     ?? context.MethodInfo.DeclaringType?.GetCustomAttributes(true).OfType<LiveOnlyAttribute>().FirstOrDefault();
        if (marker is null) return;
        operation.Extensions["x-inventoryx-live-only"] = new OpenApiBoolean(true);
        if (!string.IsNullOrWhiteSpace(marker.Reason))
            operation.Extensions["x-inventoryx-live-only-reason"] = new OpenApiString(marker.Reason);
    }
}
