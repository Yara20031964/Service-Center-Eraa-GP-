using KHDMA.Application.Interfaces.Repositories;
using KHDMA.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace KHDMA.Infrastructure.Repositories
{
    public class GenericRepository<T>: IGenericRepository<T> where T:class
    {
        AppDbContext _context;
        DbSet<T> _dbset;
        
         
        public GenericRepository(AppDbContext context)
        {
            _context = context;
            _dbset = _context.Set<T>();

        }
        public  async Task CreateAsync(T entity)
        {
           await _dbset.AddAsync(entity);
        }
        public async Task AddRangeAsync(IEnumerable<T> entities)
        {
            await _dbset.AddRangeAsync(entities);
        }
        public   void Update(T entity)
        {
              _dbset.Update(entity);
        }
        public void UpdateRange(IEnumerable<T> entities)
        {
            _dbset.UpdateRange(entities);
        }

        public void Delete(T entity)
        {
            _dbset.Remove(entity);
        }
        public void DeleteRange(IEnumerable<T> entities)
        {
            _dbset.RemoveRange(entities);
        }


        public async Task<IEnumerable<T>> GetAsync(Expression<Func<T,bool>>? expression = null, Expression<Func<T, object>>[]? includes = null, bool tracked = true)
        {
            var entities = _dbset.AsQueryable();
            if(expression != null)
            {
                entities = entities.Where(expression);
            }
            if (includes is not null)
                foreach (var i in includes)
                    entities = entities.Include(i);

            if (!tracked)
                entities = entities.AsNoTracking();

           
            
            return await entities.ToListAsync();
        }
        public async Task<T?> GetOneAsync(Expression<Func<T, bool>>? expression = null, Expression<Func<T, object>>[]? includes = null, bool tracked = true)
        {
            return (await GetAsync(expression, includes, tracked)).FirstOrDefault();

        }
    }
     
}
