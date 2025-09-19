using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using InventoryX.Application.Repository;

namespace InventoryX.Infrastructure.Persistence
{
    public class BaseRepository<TEntity>(AppDbContext context) : IBaseRepository<TEntity> where TEntity : class
    {
        private readonly AppDbContext _context = context;
        public virtual async Task<int> Add(TEntity entity)
        {
            var nameProperty = typeof(TEntity).GetProperty("Name");
            if (nameProperty is not null)
            {
                var nameValue = nameProperty.GetValue(entity)?.ToString();
                var recordExists = await _context.Set<TEntity>().AnyAsync(e => EF.Property<string>(e, "Name") == nameValue);
                if (recordExists)
                {
                    throw new InvalidOperationException("Record already exists.");
                }
            }

            // Check if foreign keys exist
            var properties = typeof(TEntity).GetProperties();
            foreach (var property in properties)
            {
                if (Attribute.IsDefined(property, typeof(ForeignKeyAttribute)))
                {
                    var foreignKeyValue = property.GetValue(entity);
                    if (foreignKeyValue != null)
                    {
                        //Removing id part of string to get correct entity type
                        var nameOfEntity = property.Name[..^2];
                        var foreignKeyProperty = typeof(TEntity).GetProperty(nameOfEntity);
                        var foreignEntityType = foreignKeyProperty.PropertyType;
                        var foreignEntity = await _context.FindAsync(foreignEntityType, foreignKeyValue);

                        if (foreignEntity == null)
                        {
                            throw new InvalidOperationException($"Foreign key entity does not exist for property {property.Name} with value {foreignKeyValue}.");
                        }
                    }
                }
            }

            await _context.AddAsync(entity);
            await _context.SaveChangesAsync();

            var idProperty = typeof(TEntity).GetProperty("Id") ?? throw new InvalidOperationException("Id property does not exist.");
            var idValue = (int)idProperty.GetValue(entity);

            return idValue;
        }

        public async Task<int> Delete(int id)
        { 
            var entityToDelete = await _context.Set<TEntity>().FindAsync(id) ?? throw new InvalidOperationException("Record does not exist");
            _context.Set<TEntity>().Remove(entityToDelete);
             
            return await _context.SaveChangesAsync();
        }

        public async Task<TEntity?> Get(int id, params Expression<Func<TEntity, object>>[] includes)
        {
            IQueryable<TEntity> query = _context.Set<TEntity>().AsNoTracking();
             
            foreach (var include in includes)
            {
                query = query.Include(include);
            }

            return await query.FirstOrDefaultAsync(e => EF.Property<int>(e, "Id") == id);
        }

        public async Task<TEntity?> Get(string columnName, object columnValue, params Expression<Func<TEntity, object>>[] includes)
        {
            IQueryable<TEntity> query = _context.Set<TEntity>().AsNoTracking();

            foreach (var include in includes)
            {
                query = query.Include(include);
            }

            // Using EF.Property to dynamically access the specified column
            return await query.FirstOrDefaultAsync(e => EF.Property<object>(e, columnName).Equals(columnValue));
        }


        public async Task<IEnumerable<TEntity>> GetAllAsync(params Expression<Func<TEntity, object>>[] includes)
        {
            IQueryable<TEntity> query = _context.Set<TEntity>();

            // Apply eager loading for each include expression
            foreach (var include in includes)
            {
                query = query.Include(include);
            }

            return await query.ToListAsync();
        }
        public async Task<int> Update(TEntity entity)
        {
            var idProperty = typeof(TEntity).GetProperty("Id") ?? throw new InvalidOperationException("Id property does not exist.");
            var idValue = idProperty.GetValue(entity) ?? throw new InvalidOperationException("Id value cannot be null.");
            var entityToUpdate = await _context.Set<TEntity>().FindAsync(idValue) ?? throw new InvalidOperationException("Record does not exist.");

            // Get the properties of the provided entity
            var properties = typeof(TEntity).GetProperties();

            foreach (var property in properties)
            {
                // Get the value of the current property in the provided entity
                var newValue = property.GetValue(entity);
                if (Attribute.IsDefined(property, typeof(ForeignKeyAttribute)))
                { 
                    if (newValue != null)
                    {
                        //Removing id part of string to get correct entity type
                        var nameOfEntity = property.Name[..^2];
                        var foreignKeyProperty = typeof(TEntity).GetProperty(nameOfEntity);
                        var foreignEntityType = foreignKeyProperty.PropertyType;
                        var foreignEntity = await _context.FindAsync(foreignEntityType, newValue);

                        if (foreignEntity == null)
                        {
                            throw new InvalidOperationException($"{property.Name} with value {newValue} does not exist.");
                        }
                    }
                }

                // If the new value is not null, update the corresponding property in the existing entity
                if (newValue != null)
                {
                    property.SetValue(entityToUpdate, newValue);
                }
            }

            return await _context.SaveChangesAsync();
        }         
    }
}
