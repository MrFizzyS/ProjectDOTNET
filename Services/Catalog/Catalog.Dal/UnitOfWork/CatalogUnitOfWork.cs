using Catalog.Dal.Repositories;

namespace Catalog.Dal.UnitOfWork;

public sealed class CatalogUnitOfWork(
    AppDbContext dbContext,
    IGenericRepository<Catalog.Domain.GameSystem> gameSystems,
    IProductRepository products,
    IProductImageRepository productImages,
    IProductCategoryRepository productCategories) : ICatalogUnitOfWork
{
    public IGenericRepository<Catalog.Domain.GameSystem> GameSystems { get; } = gameSystems;
    public IProductRepository Products { get; } = products;
    public IProductImageRepository ProductImages { get; } = productImages;
    public IProductCategoryRepository ProductCategories { get; } = productCategories;

    /// <summary>One SaveChanges call commits all tracked changes made through the repositories.</summary>
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => dbContext.SaveChangesAsync(cancellationToken);
}
