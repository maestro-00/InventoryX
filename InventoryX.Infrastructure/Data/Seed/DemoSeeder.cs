using InventoryX.Domain.Models;
using InventoryX.Domain.Models.Catalog;
using InventoryX.Domain.Models.Inventory;
using InventoryX.Domain.Models.Selling;
using InventoryX.Domain.Models.Tenancy;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace InventoryX.Infrastructure.Data.Seed
{
    /// <summary>
    /// Portfolio demo data: one Ghanaian retailer with products, stock, and sample sales.
    /// Gated by DEMO_MODE=true (set in Render or local env).
    /// </summary>
    public static class DemoSeeder
    {
        public const string DemoTenantName = "Accra Mini Mart";
        public const string DemoUserEmail = "demo@inventoryx.dev";
        public const string DemoUserPassword = "Demo123!";

        public static async Task SeedAsync(
            AppDbContext context,
            UserManager<User> userManager,
            CancellationToken cancellationToken = default)
        {
            if (await context.Tenants.AnyAsync(t => t.Name == DemoTenantName, cancellationToken))
                return;

            var now = DateTime.UtcNow;
            var freePlan = await context.PlanDefinitions
                .FirstAsync(p => p.Tier == PlanTier.Free, cancellationToken);
            var ownerRole = await context.AppRoles
                .FirstAsync(r => r.Name == "Owner", cancellationToken);
            var taxTreatment = await context.TaxTreatments
                .FirstAsync(t => t.Code == "GH-STD", cancellationToken);

            var tenantId = Guid.NewGuid();
            var tenant = new Tenant
            {
                Id = tenantId,
                Name = DemoTenantName,
                Country = "GH",
                Currency = "GHS",
                BusinessType = BusinessType.Retail,
                SampleDataLoaded = true,
                CreatedAt = now,
            };
            context.Tenants.Add(tenant);

            context.Subscriptions.Add(new Subscription
            {
                TenantId = tenantId,
                PlanDefinitionId = freePlan.Id,
                Status = SubscriptionStatus.Active,
                CurrentPeriodStart = now,
                CurrentPeriodEnd = now.AddMonths(1),
                CreatedAt = now,
            });

            var user = new User
            {
                UserName = DemoUserEmail,
                Email = DemoUserEmail,
                EmailConfirmed = true,
                Name = "Demo Owner",
                TenantId = tenantId,
                IsOwner = true,
                RoleId = ownerRole.Id,
                LocationScope = "*",
                Status = UserStatus.Active,
            };
            var createResult = await userManager.CreateAsync(user, DemoUserPassword);
            if (!createResult.Succeeded)
                throw new InvalidOperationException(
                    $"Demo user creation failed: {string.Join("; ", createResult.Errors.Select(e => e.Description))}");

            var locationId = Guid.NewGuid();
            context.Locations.Add(new Location
            {
                Id = locationId,
                TenantId = tenantId,
                Name = "Main Shop",
                Kind = LocationKind.Shop,
                Address = "Oxford Street, Osu, Accra",
                CreatedAt = now,
            });

            var registerId = Guid.NewGuid();
            context.Registers.Add(new Register
            {
                Id = registerId,
                TenantId = tenantId,
                LocationId = locationId,
                Name = "Till 1",
                CreatedAt = now,
            });

            var shiftId = Guid.NewGuid();
            context.Shifts.Add(new Shift
            {
                Id = shiftId,
                TenantId = tenantId,
                RegisterId = registerId,
                OpenedBy = user.Id,
                OpenedAt = now.AddHours(-4),
                OpeningFloat = 100m,
                Status = ShiftStatus.Open,
                CreatedAt = now,
            });

            var productDefs = new (string Name, string Sku, decimal Price, decimal Cost, decimal Qty)[]
            {
                ("Peak Milk 400g", "PEAK-400", 8.50m, 6.00m, 48),
                ("Gino Tomato Paste 70g", "GINO-70", 3.00m, 2.10m, 120),
                ("Indomie Chicken 70g", "INDO-70", 2.50m, 1.80m, 200),
                ("Voltic Water 1.5L", "VOLT-15", 4.00m, 2.50m, 72),
                ("Blue Band Margarine 450g", "BB-450", 18.00m, 14.00m, 36),
                ("Kivo Gari 1kg", "KIVO-1K", 12.00m, 9.00m, 60),
                ("Ideal Milk 170g", "IDEAL-170", 5.50m, 4.00m, 90),
                ("Yazz Sanitary Pads", "YAZZ-PAD", 15.00m, 11.00m, 40),
                ("Sunlight Soap 200g", "SUN-200", 4.50m, 3.20m, 80),
                ("Fan Yogo Vanilla", "FAN-YOG", 3.50m, 2.40m, 100),
            };

            var products = new List<Product>();
            foreach (var (name, sku, price, cost, qty) in productDefs)
            {
                var productId = Guid.NewGuid();
                products.Add(new Product
                {
                    Id = productId,
                    TenantId = tenantId,
                    Name = name,
                    Sku = sku,
                    SellingPrice = price,
                    CostPrice = cost,
                    TaxTreatmentId = taxTreatment.Id,
                    TrackingMode = TrackingMode.Simple,
                    IsSampleData = true,
                    CreatedAt = now,
                });

                context.StockLevels.Add(new StockLevel
                {
                    TenantId = tenantId,
                    ProductId = productId,
                    LocationId = locationId,
                    QtyOnHand = qty,
                    AvgUnitCost = cost,
                    CreatedAt = now,
                });
            }

            context.Products.AddRange(products);
            await context.SaveChangesAsync(cancellationToken);

            await SeedSampleSalesAsync(context, tenantId, locationId, registerId, shiftId, user.Id, products, now, cancellationToken);
        }

        private static async Task SeedSampleSalesAsync(
            AppDbContext context,
            Guid tenantId,
            Guid locationId,
            Guid registerId,
            Guid shiftId,
            string cashierId,
            List<Product> products,
            DateTime now,
            CancellationToken cancellationToken)
        {
            var sales = new[]
            {
                (Product: products[0], Qty: 2m, Cash: 25m),
                (Product: products[2], Qty: 5m, Cash: 15m),
                (Product: products[3], Qty: 3m, Cash: 20m),
            };

            long receiptSeq = 1;
            foreach (var (product, qty, cash) in sales)
            {
                var subtotal = product.SellingPrice * qty;
                var levies = subtotal * 0.06m;
                var taxTotal = (subtotal + levies) * 0.15m;
                var grandTotal = subtotal + levies + taxTotal;
                var occurredAt = now.AddHours(-receiptSeq);

                var saleId = Guid.NewGuid();
                context.Sales.Add(new Sale
                {
                    Id = saleId,
                    TenantId = tenantId,
                    LocationId = locationId,
                    RegisterId = registerId,
                    ShiftId = shiftId,
                    CashierId = cashierId,
                    ClientSaleId = Guid.NewGuid(),
                    Subtotal = subtotal,
                    TaxTotal = taxTotal + levies,
                    GrandTotal = grandTotal,
                    ChangeGiven = Math.Max(0, cash - grandTotal),
                    OccurredAt = occurredAt,
                    CreatedAt = occurredAt,
                    Lines =
                    [
                        new SaleLine
                        {
                            TenantId = tenantId,
                            ProductId = product.Id,
                            Qty = qty,
                            UnitPrice = product.SellingPrice,
                            TaxAmount = taxTotal + levies,
                            LineTotal = grandTotal,
                            ProductName = product.Name,
                            TaxComponents = "[]",
                            CreatedAt = occurredAt,
                        },
                    ],
                    Payments =
                    [
                        new SalePayment
                        {
                            TenantId = tenantId,
                            Tender = TenderType.Cash,
                            Amount = cash,
                            CreatedAt = occurredAt,
                        },
                    ],
                });

                context.Receipts.Add(new Receipt
                {
                    TenantId = tenantId,
                    SaleId = saleId,
                    SequenceNumber = receiptSeq,
                    Number = $"RCP-{receiptSeq:D6}",
                    PayloadJson = $"{{\"saleId\":\"{saleId}\",\"total\":{grandTotal}}}",
                    CreatedAt = occurredAt,
                });

                var stock = await context.StockLevels
                    .IgnoreQueryFilters()
                    .FirstAsync(s => s.TenantId == tenantId && s.ProductId == product.Id && s.LocationId == locationId, cancellationToken);
                stock.QtyOnHand -= qty;

                context.StockMovements.Add(new StockMovement
                {
                    TenantId = tenantId,
                    Type = MovementType.Sale,
                    ProductId = product.Id,
                    LocationId = locationId,
                    QtyDelta = -qty,
                    UnitCost = product.CostPrice,
                    UserId = cashierId,
                    CorrelationId = saleId,
                    OccurredAt = occurredAt,
                    CreatedAt = occurredAt,
                });

                receiptSeq++;
            }

            await context.SaveChangesAsync(cancellationToken);
        }
    }
}
