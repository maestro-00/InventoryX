using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using InventoryX.Domain.Models;

namespace InventoryX.Application.Services.IServices
{
    public interface IPurchaseService
    {
        Task<int> AddPurchase(Purchase entity);
        Task<IEnumerable<Purchase>> GetAllPurchases();
        Task<Purchase?> GetPurchase(int id);
        Task<int> UpdatePurchase(Purchase entity);
        Task<int> DeletePurchase(int id);
    }
}
