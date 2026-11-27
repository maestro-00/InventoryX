using System.Text.Json;
using FluentValidation;
using FluentValidation.Results;
using InventoryX.Presentation.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace InventoryX.Presentation.Tests.Middleware;

public class ProblemDetailsMiddlewareTests
{
    [Fact]
    public async Task Message_only_validation_exposes_actionable_detail()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        context.TraceIdentifier = "sale-trace";
        var middleware = new ProblemDetailsMiddleware(
            _ => throw new ValidationException("Tender amounts must be greater than zero."),
            NullLogger<ProblemDetailsMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        context.Response.Body.Position = 0;
        using var body = await JsonDocument.ParseAsync(context.Response.Body);
        Assert.Equal(400, context.Response.StatusCode);
        Assert.Equal("Tender amounts must be greater than zero.", body.RootElement.GetProperty("detail").GetString());
        Assert.Equal("sale-trace", body.RootElement.GetProperty("traceId").GetString());
    }

    [Fact]
    public async Task Field_validation_preserves_field_messages()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var middleware = new ProblemDetailsMiddleware(
            _ => throw new ValidationException([new ValidationFailure("Payments[0].Amount", "Enter a positive amount.")]),
            NullLogger<ProblemDetailsMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        context.Response.Body.Position = 0;
        using var body = await JsonDocument.ParseAsync(context.Response.Body);
        Assert.Equal("Enter a positive amount.", body.RootElement.GetProperty("errors").GetProperty("Payments[0].Amount")[0].GetString());
    }
}
