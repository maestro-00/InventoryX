using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using InventoryX.Application.Repository;
using InventoryX.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace InventoryX.Infrastructure.Persistence
{
    public class SalePurchaseRepository<T>(AppDbContext appDbContext) : BaseRepository<T>(appDbContext), ISalePurchaseRepository<T> where T : class
    {
        private readonly AppDbContext _appDbContext = appDbContext;

        public override async Task<int> Add(T entity)
        {
            // Check if foreign keys exist
            var properties = typeof(T).GetProperties();
            foreach (var property in properties)
            {
                if (Attribute.IsDefined(property, typeof(ForeignKeyAttribute)))
                {
                    var foreignKeyValue = property.GetValue(entity);
                    if (foreignKeyValue != null)
                    {
                        //Removing id part of string to get correct entity type
                        var nameOfEntity = property.Name[..^2];
                        var foreignKeyProperty = typeof(T).GetProperty(nameOfEntity);
                        var foreignEntityType = foreignKeyProperty.PropertyType;
                        var foreignEntity = await _appDbContext.FindAsync(foreignEntityType, foreignKeyValue);

                        if (foreignEntity == null)
                        {
                            throw new InvalidOperationException($"{property.Name} with value {foreignKeyValue} does not exist.");
                        }
                    }
                }
            }
            await _appDbContext.AddAsync(entity);
            await _appDbContext.SaveChangesAsync();

            var idProperty = typeof(T).GetProperty("Id") ?? throw new InvalidOperationException("Id property does not exist.");
            var idValue = (int)idProperty.GetValue(entity);

            return idValue;
        }
    }
}
