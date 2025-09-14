using System.Linq.Expressions;

namespace InventoryX.Application.Repository
{
    public interface IBaseRepository<TEntity>
    {
        Task<int> Add(TEntity entity);
        Task<IEnumerable<TEntity>> GetAllAsync(params Expression<Func<TEntity, object>>[] includes);
        Task<TEntity> Get(int id, params Expression<Func<TEntity, object>>[] includes);
        Task<TEntity> Get(string columnName, object columnValue, params Expression<Func<TEntity, object>>[] includes);
        Task<int> Update(TEntity entity);
        Task<int> Delete(int id);
    }
}
