using MongoDB.Bson;
using Reviews.Application.Common.Models;

namespace Reviews.Application.Common.Interfaces;

public interface IReviewQueries
{
    Task<ReviewDto?> GetByIdAsync(ObjectId id, CancellationToken cancellationToken = default);

    Task<PagedResult<ReviewDto>> GetListAsync(
        string? productId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);
}
