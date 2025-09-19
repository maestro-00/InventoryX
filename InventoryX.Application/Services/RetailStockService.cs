using InventoryX.Application.Services.IServices;
using InventoryX.Domain.Models; 
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using InventoryX.Application.Repository;

namespace InventoryX.Application.Services
{
    public class RetailStockService(IBaseRepository<RetailStock> repository) : IRetailStockService
    {
        private readonly IBaseRepository<RetailStock> _repository = repository;
        public Task<int> AddRetailStock(RetailStock entity)
        {
            return _repository.Add(entity);
        }

        public Task<int> DeleteRetailStock(int id)
        {
            return _repository.Delete(id);
        }

        public Task<IEnumerable<RetailStock>> GetAllRetailStock()
        {
            return _repository.GetAllAsync();
        }

        public Task<RetailStock?> GetRetailStock(int id)
        {
            return _repository.Get(id);
        }

        public Task<RetailStock?> GetRetailStock(string columnName, object columnValue)
        {
            return _repository.Get(columnName, columnValue); 
        }

        public Task<int> UpdateRetailStock(RetailStock entity)
        {
            return _repository.Update(entity);
        }
    }
}
