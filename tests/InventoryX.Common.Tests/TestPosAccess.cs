using InventoryX.Application.Services;
using InventoryX.Application.Services.IServices;
using InventoryX.Domain.Models.Tenancy;
using InventoryX.Infrastructure.Data;

namespace InventoryX.Common.Tests;

public static class TestPosAccess
{
    public static Role AddRole(AppDbContext context, string name, Permission permissions)
    {
        var role = new Role { Name = name, Permissions = permissions, IsSystem = true };
        context.AppRoles.Add(role);
        return role;
    }

    public static PosAccess For(AppDbContext context, ITenantContext tenant, string role, Permission permissions)
    {
        tenant.Role = role;
        if (!context.AppRoles.Local.Any(item => item.Name == role)
            && !context.AppRoles.Any(item => item.Name == role))
            AddRole(context, role, permissions);
        if (context.ChangeTracker.HasChanges())
            context.SaveChanges();
        return new PosAccess(context, tenant);
    }

    public static PosAccess Cashier(AppDbContext context, ITenantContext tenant) =>
        For(context, tenant, "Cashier", Permission.Sell | Permission.Discount | Permission.Refund);

    public static PosAccess Manager(AppDbContext context, ITenantContext tenant) =>
        For(context, tenant, "Manager",
            Permission.Sell | Permission.Refund | Permission.Discount | Permission.ViewReports);

    public static PosAccess ReadOnly(AppDbContext context, ITenantContext tenant) =>
        For(context, tenant, "ReadOnly", Permission.ViewReports);
}
