using System.Linq.Expressions;

namespace InventoryX.Application.Repository
{
    public interface ISalePurchaseRepository<T> where T : class
    {
        Task<int> Add(T entity);
        Task<IEnumerable<T>> GetAllAsync(params Expression<Func<T, object>>[] includes);
        Task<T?> Get(int id, params Expression<Func<T, object>>[] includes);
        Task<int> Update(T entity);
        Task<int> Delete(int id);
    }
}
