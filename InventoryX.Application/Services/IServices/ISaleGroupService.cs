using InventoryX.Domain.Models;

namespace InventoryX.Application.Services.IServices;

public interface ISaleGroupService
{
    Task<int> AddSaleGroup(SaleGroup entity);
    Task<IEnumerable<SaleGroup>> GetAllSaleGroups();
    Task<SaleGroup?> GetSaleGroup(int id);
    Task<int> UpdateSaleGroup(SaleGroup entity);
    Task<int> DeleteSaleGroup(int id);
}
