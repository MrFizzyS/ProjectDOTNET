using Catalog.Domain;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Dal.Repositories;

public sealed class ProductImageRepository(AppDbContext dbContext)
    : BaseEfRepository<ProductImage>(dbContext), IProductImageRepository
{
    public Task LoadForProductAsync(Product product, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(product);
        return DbContext.Entry(product)
            .Collection(item => item.Images)
            .Query()
            .OrderBy(image => image.SortOrder)
            .LoadAsync(cancellationToken);
    }
}
