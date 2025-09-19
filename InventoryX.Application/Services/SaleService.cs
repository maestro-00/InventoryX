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
    public class SaleService(ISalePurchaseRepository<Sale> repository, IAuthService authService) : ISaleService
    {
        private readonly ISalePurchaseRepository<Sale> _repository = repository;
        private readonly IAuthService _authService = authService;
        public async Task<int> AddSale(Sale entity)
        {
            var user = await _authService.GetAuthenticatedUser();
            entity.UserId = user.Id;
            return await _repository.Add(entity);
        }

        public Task<int> DeleteSale(int id)
        {
            return _repository.Delete(id);
        }

        public Task<IEnumerable<Sale>> GetAllSales()
        {
            return _repository.GetAllAsync(
                    i => i.InventoryItem,
                    i => i.Seller
                );
        }

        public Task<Sale?> GetSale(int id)
        {
            return _repository.Get(
                id,
                 i => i.InventoryItem,
                 i => i.Seller
                );
        }

        public Task<int> UpdateSale(Sale entity)
        {
            return _repository.Update(entity);
        }
    }
}
