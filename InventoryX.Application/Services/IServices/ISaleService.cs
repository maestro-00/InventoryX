using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using InventoryX.Application.DTOs.Sales;
using InventoryX.Domain.Models;

namespace InventoryX.Application.Services.IServices
{
    public interface ISaleService
    {
        Task<int> AddSale(Sale entity);
        Task<IEnumerable<Sale>> GetAllSales();
        Task<Sale?> GetSale(int id);
        Task<List<Sale>> GetSalesByGroupId(int id);
        Task<int> UpdateSale(Sale entity);
        Task<int> DeleteSale(int id);
        Task<GetSaleStatsDto> GetSaleStats(CancellationToken token);
    }
}
