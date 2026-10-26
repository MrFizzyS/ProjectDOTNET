using Microsoft.EntityFrameworkCore;
using Catalog.Dal.Specifications;

namespace Catalog.Dal.Repositories;

/// <summary>Shared DbSet-backed CRUD implementation. Persistence is committed only by Unit of Work.</summary>
public class BaseEfRepository<TEntity>(AppDbContext dbContext) : IGenericRepository<TEntity>
    where TEntity : class
{
    protected AppDbContext DbContext { get; } = dbContext;
    protected DbSet<TEntity> DbSet { get; } = dbContext.Set<TEntity>();

    public async Task<TEntity?> GetByIdAsync(object[] keyValues, CancellationToken cancellationToken = default)
        => await DbSet.FindAsync(keyValues, cancellationToken);

    public async Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken cancellationToken = default)
        => await DbSet.AsNoTracking().ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<TEntity>> ListAsync(ISpecification<TEntity> specification, CancellationToken cancellationToken = default)
        => await SpecificationEvaluator.GetQuery(DbSet.AsNoTracking(), specification).ToListAsync(cancellationToken);

    public Task<int> CountAsync(ISpecification<TEntity> specification, CancellationToken cancellationToken = default)
        => SpecificationEvaluator.GetCountQuery(DbSet.AsNoTracking(), specification).CountAsync(cancellationToken);

    public async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        await DbSet.AddAsync(entity, cancellationToken);
    }

    public void Update(TEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        DbSet.Update(entity);
    }

    public void Remove(TEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        DbSet.Remove(entity);
    }
}
