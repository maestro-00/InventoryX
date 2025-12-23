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
}
