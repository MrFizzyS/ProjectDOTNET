using Catalog.Domain;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Dal.Repositories;

public sealed class ProductRepository(AppDbContext dbContext)
    : BaseEfRepository<Product>(dbContext), IProductRepository
{
    public Task<bool> SkuExistsAsync(string sku, Guid? excludingProductId = null, CancellationToken cancellationToken = default)
        => DbSet.AnyAsync(product => product.Sku == sku &&
            (excludingProductId == null || product.ProductId != excludingProductId), cancellationToken);

    public Task<Product?> GetWithCatalogAsync(Guid productId, CancellationToken cancellationToken = default)
        => DbSet
            .AsNoTracking()
            .Include(product => product.GameSystem)
            .Include(product => product.Detail)
            .Include(product => product.ProductCategories)
                .ThenInclude(assignment => assignment.Category)
            .SingleOrDefaultAsync(product => product.ProductId == productId, cancellationToken);
}
