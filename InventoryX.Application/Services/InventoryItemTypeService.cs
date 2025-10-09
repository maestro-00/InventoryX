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
    public class InventoryItemTypeService(IBaseRepository<InventoryItemType> baseRepository) : IInventoryItemTypeService
    {
        private readonly IBaseRepository<InventoryItemType> _repository = baseRepository;
        public Task<int> AddInventoryItemType(InventoryItemType entity)
        {
            return _repository.Add(entity);
        }

        public Task<int> DeleteInventoryItemType(int id)
        {
            return _repository.Delete(id);
        }

        public Task<IEnumerable<InventoryItemType>> GetAllInventoryItemTypes()
        {
            return _repository.GetAllAsync();
        }

        public Task<InventoryItemType?> GetInventoryItemType(int id)
        {
            return _repository.Get(id);
        }

        public Task<int> UpdateInventoryItemType(InventoryItemType entity)
        {
            return _repository.Update(entity);
        }
    }
}
