using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using InventoryX.Application.Repository;
using InventoryX.Application.Services.IServices;
using InventoryX.Domain.Models;

namespace InventoryX.Application.Services
{
    public class InventoryItemService(IBaseRepository<InventoryItem> repository) : IInventoryItemService
    {
        private readonly IBaseRepository<InventoryItem> _repository = repository;
        public Task<int> AddInventoryItem(InventoryItem entity)
        {
            return _repository.Add(entity);
        }

        public Task<int> DeleteInventoryItem(int id)
        {
            return _repository.Delete(id);
        }

        public Task<IEnumerable<InventoryItem>> GetAllInventoryItems()
        {
            return _repository.GetAllAsync(
                    i => i.Type
                );
        }

        public Task<InventoryItem?> GetInventoryItem(int id)
        {
            return _repository.Get(
                id,
                i => i.Type
                );
        }

        public Task<int> UpdateInventoryItem(InventoryItem entity)
        {
            return _repository.Update(entity);
        }
    }
}
