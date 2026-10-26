using MongoDB.Bson;
using MongoDB.Driver;
using Reviews.Domain.Entities;
using Reviews.Domain.Interfaces;

namespace Reviews.Infrastructure.Persistence;

public sealed class MongoReviewRepository(MongoDbContext context) : IReviewRepository
{
    public Task AddAsync(Review review, CancellationToken cancellationToken = default)
        => context.Reviews.InsertOneAsync(review, cancellationToken: cancellationToken);

    public async Task<Review?> GetByIdAsync(ObjectId id, CancellationToken cancellationToken = default)
        => await context.Reviews.Find(review => review.Id == id).FirstOrDefaultAsync(cancellationToken);

    public Task<bool> ExistsAsync(
        string productId,
        string authorCustomerId,
        CancellationToken cancellationToken = default)
        => context.Reviews.Find(review =>
                review.ProductId == productId
                && review.Author.CustomerId == authorCustomerId)
            .AnyAsync(cancellationToken);

    public async Task<bool> UpdateAsync(
        Review review,
        int expectedVersion,
        CancellationToken cancellationToken = default)
    {
        review.BumpVersion();
        var filter = Builders<Review>.Filter.Eq(item => item.Id, review.Id)
                     & Builders<Review>.Filter.Eq(item => item.Version, expectedVersion);
        var result = await context.Reviews.ReplaceOneAsync(
            filter,
            review,
            cancellationToken: cancellationToken);

        return result.ModifiedCount == 1;
    }

    public async Task<bool> DeleteAsync(
        ObjectId id,
        int expectedVersion,
        CancellationToken cancellationToken = default)
    {
        var filter = Builders<Review>.Filter.Eq(item => item.Id, id)
                     & Builders<Review>.Filter.Eq(item => item.Version, expectedVersion);
        var result = await context.Reviews.DeleteOneAsync(filter, cancellationToken);
        return result.DeletedCount == 1;
    }
}
