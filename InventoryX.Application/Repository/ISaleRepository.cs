using InventoryX.Domain.Models;

namespace InventoryX.Application.Repository;

public interface ISaleRepository
{
    Task<List<Sale>> GetSaleBySaleGroupId(int id);
}
