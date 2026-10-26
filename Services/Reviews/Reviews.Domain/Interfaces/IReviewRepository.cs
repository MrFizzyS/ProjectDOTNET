using MongoDB.Bson;
using Reviews.Domain.Entities;

namespace Reviews.Domain.Interfaces;

public interface IReviewRepository
{
    Task AddAsync(Review review, CancellationToken cancellationToken = default);

    Task<Review?> GetByIdAsync(ObjectId id, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(
        string productId,
        string authorCustomerId,
        CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(
        Review review,
        int expectedVersion,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        ObjectId id,
        int expectedVersion,
        CancellationToken cancellationToken = default);
}
