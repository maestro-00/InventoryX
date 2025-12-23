using InventoryX.Application.Repository;
using InventoryX.Application.Services.IServices;
using InventoryX.Domain.Models;

namespace InventoryX.Application.Services;

public class SaleGroupService(IBaseRepository<SaleGroup> repository) : ISaleGroupService
{
    public async Task<int> AddSaleGroup(SaleGroup entity)
    {
        return await repository.Add(entity);
    }

    public async Task<IEnumerable<SaleGroup>> GetAllSaleGroups()
    {
        return await repository.GetAllAsync();
    }

    public async Task<SaleGroup?> GetSaleGroup(int id)
    {
        return await repository.Get(id);
    }

    public async Task<int> UpdateSaleGroup(SaleGroup entity)
    {
        return await repository.Update(entity);
    }

    public async Task<int> DeleteSaleGroup(int id)
    {
        return await repository.Delete(id);
    }
}
