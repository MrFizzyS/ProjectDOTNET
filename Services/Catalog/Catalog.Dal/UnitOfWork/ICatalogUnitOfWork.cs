using Catalog.Dal.Repositories;
using Catalog.Domain;

namespace Catalog.Dal.UnitOfWork;

/// <summary>Coordinates catalog repositories that share one scoped AppDbContext.</summary>
public interface ICatalogUnitOfWork
{
    IGenericRepository<GameSystem> GameSystems { get; }
    IProductRepository Products { get; }
    IProductImageRepository ProductImages { get; }
    IProductCategoryRepository ProductCategories { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
