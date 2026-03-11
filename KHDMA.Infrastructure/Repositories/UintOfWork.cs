using KHDMA.Application.Interfaces.Repositories;
using KHDMA.Infrastructure.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KHDMA.Infrastructure.Repositories
{
    public class UnitOfWork : IUnitOfWork

    {
        private readonly AppDbContext _context;
       

        public UnitOfWork(AppDbContext context)
        {
            _context = context;
        }
        public async Task<int> CommitAsync()
        {
            try
            {
                return await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                throw new Exception($"Commit failed: {ex.Message}", ex);
            }
        }
        public IGenericRepository<TEntity> Repository<TEntity>() where TEntity : class
        {
            return new GenericRepository<TEntity>(_context);
        }
    }
}
