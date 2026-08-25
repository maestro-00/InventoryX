using InventoryX.Application.Exceptions;
using InventoryX.Application.Repository;
using InventoryX.Application.Services.IServices;
using InventoryX.Domain.Models.Selling;
using InventoryX.Domain.Models.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace InventoryX.Application.Services;

public sealed class PosAccess(IAppDbContext context, ITenantContext tenantContext) : IPosAccess
{
    private Permission? _loaded;

    public async Task<bool> HasAsync(Permission permission, CancellationToken cancellationToken = default) =>
        (await LoadAsync(cancellationToken) & permission) == permission;

    public async Task RequireAsync(Permission permission, CancellationToken cancellationToken = default)
    {
        if (!await HasAsync(permission, cancellationToken))
            throw new CustomException("You are not permitted to perform this action.", 403);
    }

    public Task<bool> CanViewOthersAsync(CancellationToken cancellationToken = default) =>
        HasAsync(Permission.ViewReports, cancellationToken);

    public async Task EnsureCanViewSalesAsync(CancellationToken cancellationToken = default)
    {
        if (await HasAsync(Permission.Sell, cancellationToken) ||
            await HasAsync(Permission.ViewReports, cancellationToken))
            return;
        throw new CustomException("You are not permitted to view sales.", 403);
    }

    public async Task EnsureCanOperateShiftAsync(Shift shift, CancellationToken cancellationToken = default)
    {
        await RequireAsync(Permission.Sell, cancellationToken);
        if (IsOwnShift(shift) || await CanViewOthersAsync(cancellationToken))
            return;
        throw new CustomException(
            "Only the cashier who opened this shift, or a manager, can continue it.", 403);
    }

    public async Task EnsureCanViewShiftAsync(Shift shift, CancellationToken cancellationToken = default)
    {
        if (await CanViewOthersAsync(cancellationToken))
            return;
        await RequireAsync(Permission.Sell, cancellationToken);
        if (IsOwnShift(shift))
            return;
        throw new CustomException("You are not permitted to view this shift.", 403);
    }

    public string? UserId => tenantContext.UserId;

    private bool IsOwnShift(Shift shift) =>
        string.Equals(shift.OpenedBy, tenantContext.UserId, StringComparison.Ordinal);

    private async Task<Permission> LoadAsync(CancellationToken cancellationToken)
    {
        if (_loaded is not null) return _loaded.Value;
        if (string.IsNullOrWhiteSpace(tenantContext.Role))
        {
            _loaded = Permission.None;
            return _loaded.Value;
        }

        var role = await context.AppRoles.AsNoTracking()
            .FirstOrDefaultAsync(item => item.Name == tenantContext.Role, cancellationToken);
        _loaded = role?.Permissions ?? Permission.None;
        return _loaded.Value;
    }
}
