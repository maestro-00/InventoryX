using System.Text.Json;
using FluentValidation;
using InventoryX.Application.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InventoryX.Presentation.Middleware
{
    /// <summary>
    /// Converts unhandled exceptions into RFC 7807 problem-details responses:
    /// validation → 400, plan limit → 402, not found → 404, concurrency/illegal
    /// state → 409, pending approval → 423, everything else → 500.
    /// </summary>
    public class ProblemDetailsMiddleware(RequestDelegate next, ILogger<ProblemDetailsMiddleware> logger)
    {
        private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await next(context);
            }
            catch (Exception exception)
            {
                if (context.Response.HasStarted) throw;
                await WriteProblemAsync(context, exception);
            }
        }

        private async Task WriteProblemAsync(HttpContext context, Exception exception)
        {
            var problem = exception switch
            {
                ValidationException validation => new ValidationProblemDetails(
                    validation.Errors
                        .GroupBy(e => e.PropertyName)
                        .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()))
                {
                    Type = "https://inventoryx.app/problems/validation",
                    Title = "One or more validation errors occurred.",
                    Status = StatusCodes.Status400BadRequest,
                },
                PlanLimitException planLimit => new ProblemDetails
                {
                    Type = "https://inventoryx.app/problems/plan-limit",
                    Title = "Subscription plan limit reached.",
                    Status = StatusCodes.Status402PaymentRequired,
                    Detail = planLimit.Message,
                    Extensions = { ["upgradeHint"] = planLimit.UpgradeHint },
                },
                NotFoundException notFound => new ProblemDetails
                {
                    Type = "https://inventoryx.app/problems/not-found",
                    Title = "Resource not found.",
                    Status = StatusCodes.Status404NotFound,
                    Detail = notFound.Message,
                },
                ConflictException conflict => new ProblemDetails
                {
                    Type = "https://inventoryx.app/problems/conflict",
                    Title = "The request conflicts with the current state.",
                    Status = StatusCodes.Status409Conflict,
                    Detail = conflict.Message,
                },
                DbUpdateConcurrencyException => new ProblemDetails
                {
                    Type = "https://inventoryx.app/problems/concurrency",
                    Title = "The resource was modified by another request.",
                    Status = StatusCodes.Status409Conflict,
                    Detail = "Reload the resource and retry with the latest version.",
                },
                ApprovalRequiredException approval => new ProblemDetails
                {
                    Type = "https://inventoryx.app/problems/approval-required",
                    Title = "Operation parked pending approval.",
                    Status = StatusCodes.Status423Locked,
                    Detail = approval.Message,
                    Extensions = { ["pendingEntityId"] = approval.PendingEntityId },
                },
                CustomException custom => new ProblemDetails
                {
                    Type = "https://inventoryx.app/problems/error",
                    Title = "Request failed.",
                    Status = custom.StatusCode,
                    Detail = custom.Message,
                },
                _ => new ProblemDetails
                {
                    Type = "https://inventoryx.app/problems/internal",
                    Title = "An unexpected error occurred.",
                    Status = StatusCodes.Status500InternalServerError,
                },
            };

            if (problem.Status >= StatusCodes.Status500InternalServerError)
                logger.LogError(exception, "Unhandled exception for {Path}", context.Request.Path);
            else
                logger.LogInformation("Request problem {Status} for {Path}: {Detail}", problem.Status, context.Request.Path, problem.Detail);

            problem.Instance = context.Request.Path;
            problem.Extensions["traceId"] = context.TraceIdentifier;

            context.Response.Clear();
            context.Response.StatusCode = problem.Status!.Value;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsync(JsonSerializer.Serialize<object>(problem, SerializerOptions));
        }
    }
}
