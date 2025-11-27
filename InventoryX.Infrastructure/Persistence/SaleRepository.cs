using InventoryX.Application.Repository;
using InventoryX.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace InventoryX.Infrastructure.Persistence;

public class SaleRepository(AppDbContext dbContext) : ISaleRepository
{
    public async Task<List<Sale>> GetSaleBySaleGroupId(int id)
    {
        return await dbContext.Sales.Where(x => x.SaleGroupId == id).ToListAsync();
    }
}
