namespace Catalog.Dal.Repositories;

/// <summary>Asynchronous CRUD contract for entities tracked by the shared EF Core context.</summary>
public interface IGenericRepository<TEntity> where TEntity : class
{
    Task<TEntity?> GetByIdAsync(object[] keyValues, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TEntity>> ListAsync(Catalog.Dal.Specifications.ISpecification<TEntity> specification, CancellationToken cancellationToken = default);
    Task<int> CountAsync(Catalog.Dal.Specifications.ISpecification<TEntity> specification, CancellationToken cancellationToken = default);
    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);
    void Update(TEntity entity);
    void Remove(TEntity entity);
}
