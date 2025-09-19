using InventoryX.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InventoryX.Application.Services.IServices
{
    public interface IInventoryItemTypeService
    {
        Task<int> AddInventoryItemType(InventoryItemType entity);
        Task<IEnumerable<InventoryItemType>> GetAllInventoryItemTypes();
        Task<InventoryItemType?> GetInventoryItemType(int id);
        Task<int> UpdateInventoryItemType(InventoryItemType entity);
        Task<int> DeleteInventoryItemType(int id);
    }
}
