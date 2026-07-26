using InventoryX.Application.Services.IServices;
using InventoryX.Domain.Models.Tenancy;
using MediatR;

namespace InventoryX.Application.Behaviors
{
    /// <summary>Marker for commands that mutate tenant data (blocked while ReadOnly).</summary>
    public interface ITenantWriteCommand;

    /// <summary>Marker for commands that create a plan-capped entity or sale.</summary>
    public interface IPlanLimitedCommand : ITenantWriteCommand
    {
        UsageMetric Metric { get; }
    }

    /// <summary>Marker for commands gated behind a plan feature flag.</summary>
    public interface IFeatureGatedCommand : ITenantWriteCommand
    {
        PlanFeature Feature { get; }
    }

    /// <summary>
    /// Skeleton plan enforcement (T022, completed by T083/US5): write-block for
    /// ReadOnly subscriptions, entity/sale caps, feature gates → 402 problems.
    /// </summary>
    public class PlanEnforcementBehavior<TRequest, TResponse>(IPlanEnforcer planEnforcer)
        : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
    {
        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            if (request is ITenantWriteCommand)
                await planEnforcer.EnsureWritableAsync(cancellationToken);

            if (request is IFeatureGatedCommand featureGated)
                await planEnforcer.EnsureFeatureAsync(featureGated.Feature, cancellationToken);

            if (request is IPlanLimitedCommand planLimited)
                await planEnforcer.EnsureWithinLimitAsync(planLimited.Metric, cancellationToken: cancellationToken);

            return await next(cancellationToken);
        }
    }
}
