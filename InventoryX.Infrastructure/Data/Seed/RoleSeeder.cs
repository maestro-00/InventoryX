using InventoryX.Domain.Models.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace InventoryX.Infrastructure.Data.Seed
{
    /// <summary>Seeds the six fixed Cycle 1 role bundles (T019, data-model Identity & Access).</summary>
    public static class RoleSeeder
    {
        public static async Task SeedAsync(AppDbContext context, CancellationToken cancellationToken = default)
        {
            var all = Permission.Sell | Permission.Refund | Permission.Discount | Permission.VoidSale |
                      Permission.ViewProfit | Permission.ManageStock | Permission.ManagePurchasing |
                      Permission.ManagePricing | Permission.ManageUsers | Permission.ViewReports |
                      Permission.ApproveAdjustments;

            var roles = new List<Role>
            {
                new() { Name = "Owner", Permissions = all, IsSystem = true },
                new() { Name = "Administrator", Permissions = all, IsSystem = true },
                new()
                {
                    Name = "Manager",
                    Permissions = Permission.Sell | Permission.Refund | Permission.Discount | Permission.VoidSale |
                                  Permission.ViewProfit | Permission.ManageStock | Permission.ManagePurchasing |
                                  Permission.ViewReports | Permission.ApproveAdjustments,
                    MaxDiscountPercent = 30,
                    IsSystem = true,
                },
                new()
                {
                    Name = "Cashier",
                    Permissions = Permission.Sell | Permission.Refund | Permission.Discount,
                    MaxDiscountPercent = 5,
                    MaxUnauthorizedRefundAmount = 100,
                    IsSystem = true,
                },
                new()
                {
                    Name = "StockClerk",
                    Permissions = Permission.ManageStock,
                    IsSystem = true,
                },
                new()
                {
                    Name = "ReadOnly",
                    Permissions = Permission.ViewReports,
                    IsSystem = true,
                },
            };

            foreach (var role in roles)
            {
                if (!await context.AppRoles.AnyAsync(r => r.Name == role.Name, cancellationToken))
                    context.AppRoles.Add(role);
            }

            await context.SaveChangesAsync(cancellationToken);
        }
    }
}
