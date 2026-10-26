using Catalog.Domain;

namespace Catalog.Dal.Repositories;

public interface IProductRepository : IGenericRepository<Product>
{
    Task<bool> SkuExistsAsync(string sku, Guid? excludingProductId = null, CancellationToken cancellationToken = default);

    /// <summary>Loads the product and its catalog-facing relationships in one query (eager loading).</summary>
    Task<Product?> GetWithCatalogAsync(Guid productId, CancellationToken cancellationToken = default);
}
