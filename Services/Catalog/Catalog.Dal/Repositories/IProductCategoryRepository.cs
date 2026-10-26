using Catalog.Domain;

namespace Catalog.Dal.Repositories;

public interface IProductCategoryRepository : IGenericRepository<ProductCategory>
{
    /// <summary>Queries the M:N relation and returns the join metadata together with category details.</summary>
    Task<IReadOnlyList<ProductCategoryAssignment>> GetAssignmentsForProductAsync(
        Guid productId, CancellationToken cancellationToken = default);
}

public sealed record ProductCategoryAssignment(
    Guid ProductId,
    int CategoryId,
    string CategoryName,
    DateTime AddedAt,
    string AssignedBy);
