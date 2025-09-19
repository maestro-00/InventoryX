using InventoryX.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InventoryX.Application.Services.IServices
{
    public interface IInventoryItemService
    {
        Task<int> AddInventoryItem(InventoryItem entity);
        Task<IEnumerable<InventoryItem>> GetAllInventoryItems();
        Task<InventoryItem?> GetInventoryItem(int id);
        Task<int> UpdateInventoryItem(InventoryItem entity);
        Task<int> DeleteInventoryItem(int id);
    }
}
