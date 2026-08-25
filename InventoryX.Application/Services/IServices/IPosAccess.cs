using InventoryX.Domain.Models.Selling;
using InventoryX.Domain.Models.Tenancy;

namespace InventoryX.Application.Services.IServices;

/// <summary>POS own-vs-manager access: Sell for till work, ViewReports to see or continue others.</summary>
public interface IPosAccess
{
    Task<bool> HasAsync(Permission permission, CancellationToken cancellationToken = default);
    Task RequireAsync(Permission permission, CancellationToken cancellationToken = default);
    Task<bool> CanViewOthersAsync(CancellationToken cancellationToken = default);
    Task EnsureCanViewSalesAsync(CancellationToken cancellationToken = default);
    Task EnsureCanOperateShiftAsync(Shift shift, CancellationToken cancellationToken = default);
    Task EnsureCanViewShiftAsync(Shift shift, CancellationToken cancellationToken = default);
    string? UserId { get; }
}
