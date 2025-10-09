using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using InventoryX.Domain.Models;

namespace InventoryX.Application.Services.IServices
{
    public interface IRetailStockService
    {
        Task<int> AddRetailStock(RetailStock entity);
        Task<IEnumerable<RetailStock>> GetAllRetailStock();
        Task<RetailStock?> GetRetailStock(int id);
        Task<RetailStock?> GetRetailStock(string columnName, object columnValue);
        Task<int> UpdateRetailStock(RetailStock entity);
        Task<int> DeleteRetailStock(int id);
    }
}
