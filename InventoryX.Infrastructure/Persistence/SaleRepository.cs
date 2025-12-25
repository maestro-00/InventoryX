using System.Linq.Expressions;
using InventoryX.Application.Repository;
using InventoryX.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace InventoryX.Infrastructure.Persistence;

public class SaleRepository(AppDbContext dbContext) : ISaleRepository
{
    public async Task<List<Sale>> GetSaleBySaleGroupId(int id, params Expression<Func<Sale, object>>[] includes)
    {
        IQueryable<Sale> query = dbContext.Sales.Where(x => x.SaleGroupId == id);

        foreach (var include in includes)
        {
            query = query.Include(include);
        }

        return await query.ToListAsync();
    }


    public async Task<int> GetTotalInventoryItems(CancellationToken token)
    {
        return await dbContext.InventoryItems.AsNoTracking().CountAsync(token);
    }

    public async Task<int> GetLowStockItems(CancellationToken token)
    {
        return await dbContext.InventoryItems.AsNoTracking().Where(item => item.TotalAmount <= item.ReOrderLevel).CountAsync(token);
    }

    public async Task<decimal> GetTodaysSales(CancellationToken token)
        {
        var todayUtc = DateTime.UtcNow.Date;
        var tomorrowUtc = todayUtc.AddDays(1);

        return await dbContext.Sales
            .AsNoTracking()
            .Where(sale => sale.Created_At >= todayUtc &&
                           sale.Created_At < tomorrowUtc)
            .SumAsync(sale => sale.SubTotal, token);
    }

    public async Task<decimal> GetTotalRevenue(CancellationToken token)
    {
        var totalSalesRevenue = await dbContext.Sales.AsNoTracking().Where(sale => sale.SaleGroupId == null)
            .SumAsync(sale => sale.SubTotal, token);
        var totalSaleRevenueBySaleGroup = await dbContext.SaleGroups.AsNoTracking().SumAsync(saleGroup => saleGroup.TotalAmount, token);
        return totalSaleRevenueBySaleGroup + totalSalesRevenue;
    }

}
