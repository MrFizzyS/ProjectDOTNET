using Catalog.Domain;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Dal.Repositories;

public sealed class ProductCategoryRepository(AppDbContext dbContext)
    : BaseEfRepository<ProductCategory>(dbContext), IProductCategoryRepository
{
    public async Task<IReadOnlyList<ProductCategoryAssignment>> GetAssignmentsForProductAsync(
        Guid productId, CancellationToken cancellationToken = default)
        => await (
            from assignment in DbContext.ProductCategories.AsNoTracking()
            join category in DbContext.Categories.AsNoTracking()
                on assignment.CategoryId equals category.CategoryId
            where assignment.ProductId == productId
            orderby category.Name
            select new ProductCategoryAssignment(
                assignment.ProductId,
                assignment.CategoryId,
                category.Name,
                assignment.AddedAt,
                assignment.AssignedBy))
            .ToListAsync(cancellationToken);
}
