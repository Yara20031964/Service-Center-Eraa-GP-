namespace KHDMA.Application.Interfaces.Repositories
{
    public interface IUnitOfWork
    {
        

        Task<int> CommitAsync();
        IGenericRepository<TEntity> Repository<TEntity>() where TEntity : class;
    }
 
    

 
}