using System.Linq.Expressions;
using InventoryX.Domain.Models;

namespace InventoryX.Application.Repository;

public interface ISaleRepository
{
    Task<List<Sale>> GetSaleBySaleGroupId(int id, params Expression<Func<Sale, object>>[] includes);
    Task<int> GetTotalInventoryItems(CancellationToken token);
    Task<int> GetLowStockItems(CancellationToken token);
    Task<decimal> GetTodaysSales(CancellationToken token);
    Task<decimal> GetTotalRevenue(CancellationToken token);

}
