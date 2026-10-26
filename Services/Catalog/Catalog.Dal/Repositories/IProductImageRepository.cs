using Catalog.Domain;

namespace Catalog.Dal.Repositories;

public interface IProductImageRepository : IGenericRepository<ProductImage>
{
    /// <summary>Explicitly loads the image collection for an already tracked product.</summary>
    Task LoadForProductAsync(Product product, CancellationToken cancellationToken = default);
}
