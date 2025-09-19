using InventoryX.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InventoryX.Application.Services.IServices
{
    public interface ISaleService
    {
        Task<int> AddSale(Sale entity);
        Task<IEnumerable<Sale>> GetAllSales();
        Task<Sale?> GetSale(int id);
        Task<int> UpdateSale (Sale entity);
        Task<int> DeleteSale(int id);
    }
}
