using System.Reflection;
using InventoryX.Application.Exceptions;
using InventoryX.Application.Repository;
using InventoryX.Application.Services.IServices;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InventoryX.Presentation.Middleware;

/// <summary>Enforces the Manager JWT location_scope claim before location-bound requests execute.</summary>
public sealed class LocationScopeAuthorizationHandler<TRequest, TResponse>(
    ITenantContext tenantContext,
    IAppDbContext context) : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(tenantContext.Role, "Manager", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(tenantContext.LocationScope, "*", StringComparison.Ordinal))
            return await next(cancellationToken);

        var allowed = ParseScope(tenantContext.LocationScope);
        var locations = await ResolveLocationsAsync(request, cancellationToken);
        if (locations.RequiresExplicitLocation && locations.Ids.Count == 0)
            throw new CustomException("A scoped manager must select an allowed location.", 403);
        if (locations.Ids.Any(id => !allowed.Contains(id)))
            throw new CustomException("The requested operation is outside the manager's location scope.", 403);
        return await next(cancellationToken);
    }

    private static HashSet<Guid> ParseScope(string? value) =>
        (value ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Select(text => Guid.TryParse(text, out var id) ? id : Guid.Empty)
        .Where(id => id != Guid.Empty).ToHashSet();

    private async Task<(HashSet<Guid> Ids, bool RequiresExplicitLocation)> ResolveLocationsAsync(
        TRequest request, CancellationToken cancellationToken)
    {
        var type = request.GetType();
        var ids = new HashSet<Guid>();
        var directNames = new[] { "LocationId", "FromLocationId", "ToLocationId" };
        var hasLocationSelector = false;
        foreach (var name in directNames)
        {
            var property = type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
            if (property is null) continue;
            hasLocationSelector = true;
            if (property.GetValue(request) is Guid directLocationId && directLocationId != Guid.Empty)
                ids.Add(directLocationId);
        }

        async Task AddRegisterAsync(Guid registerKey)
        {
            var location = await context.Registers.Where(r => r.Id == registerKey).Select(r => (Guid?)r.LocationId)
                .SingleOrDefaultAsync(cancellationToken);
            if (location is Guid value) ids.Add(value);
        }
        if (Value(type, request, "RegisterId") is Guid registerId) await AddRegisterAsync(registerId);
        if (Value(type, request, "SaleId") is Guid saleId)
        {
            var location = await context.Sales.Where(s => s.Id == saleId).Select(s => (Guid?)s.LocationId).SingleOrDefaultAsync(cancellationToken);
            if (location is Guid value) ids.Add(value);
        }
        if (Value(type, request, "TransferId") is Guid transferId)
        {
            var transfer = await context.StockTransfers.Where(t => t.Id == transferId)
                .Select(t => new { t.FromLocationId, t.ToLocationId }).SingleOrDefaultAsync(cancellationToken);
            if (transfer is not null) { ids.Add(transfer.FromLocationId); ids.Add(transfer.ToLocationId); }
        }
        if (Value(type, request, "CountId") is Guid countId)
        {
            var location = await context.StockCounts.Where(c => c.Id == countId).Select(c => (Guid?)c.LocationId).SingleOrDefaultAsync(cancellationToken);
            if (location is Guid value) ids.Add(value);
        }
        if (Value(type, request, "AdjustmentId") is Guid adjustmentId)
        {
            var location = await context.StockAdjustments.Where(a => a.Id == adjustmentId).Select(a => (Guid?)a.LocationId).SingleOrDefaultAsync(cancellationToken);
            if (location is Guid value) ids.Add(value);
        }
        if (Value(type, request, "MovementId") is Guid movementId)
        {
            var location = await context.StockMovements.Where(m => m.Id == movementId).Select(m => (Guid?)m.LocationId).SingleOrDefaultAsync(cancellationToken);
            if (location is Guid value) ids.Add(value);
        }
        if ((type.Name is "GetSaleQuery" or "GetHeldSaleQuery") && Value(type, request, "Id") is Guid saleQueryId)
        {
            var location = await context.Sales.Where(s => s.Id == saleQueryId).Select(s => (Guid?)s.LocationId).SingleOrDefaultAsync(cancellationToken);
            if (location is Guid value) ids.Add(value);
        }
        return (ids, hasLocationSelector);
    }

    private static object? Value(Type type, object request, string name) =>
        type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance)?.GetValue(request);
}
