using MongoDB.Bson;
using MongoDB.Driver;
using Reviews.Application.Common.Interfaces;
using Reviews.Application.Common.Models;
using Reviews.Domain.Entities;

namespace Reviews.Infrastructure.Persistence;

public sealed class MongoReviewQueries(MongoDbContext context) : IReviewQueries
{
    private static readonly ProjectionDefinition<Review, ReviewDto> ReviewProjection =
        new BsonDocumentProjectionDefinition<Review, ReviewDto>(new BsonDocument
    {
        { "_id", 1 },
        { "productId", 1 },
        { "productName", 1 },
        { "authorCustomerId", "$author.customerId" },
        { "authorHandle", "$author.handle" },
        { "orderId", 1 },
        { "purchaseStatusSnapshot", 1 },
        { "rating", 1 },
        { "aspects", 1 },
        { "body", 1 },
        {
            "replies",
            new BsonDocument("$map", new BsonDocument
            {
                { "input", "$replies" },
                { "as", "reply" },
                {
                    "in",
                    new BsonDocument
                    {
                        { "authorCustomerId", "$$reply.author.customerId" },
                        { "authorHandle", "$$reply.author.handle" },
                        { "body", "$$reply.body" },
                        { "createdAt", "$$reply.createdAt" }
                    }
                }
            })
        },
        { "createdAt", 1 },
        { "updatedAt", 1 },
        { "version", 1 }
    });

    public async Task<ReviewDto?> GetByIdAsync(ObjectId id, CancellationToken cancellationToken = default)
        => await context.Reviews
            .Find(review => review.Id == id)
            .Project(ReviewProjection)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<PagedResult<ReviewDto>> GetListAsync(
        string? productId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var filter = string.IsNullOrWhiteSpace(productId)
            ? Builders<Review>.Filter.Empty
            : Builders<Review>.Filter.Eq(review => review.ProductId, productId);
        var totalCount = await context.Reviews.CountDocumentsAsync(
            filter,
            cancellationToken: cancellationToken);
        var skip = checked((int)((long)(pageNumber - 1) * pageSize));
        var items = await context.Reviews
            .Find(filter)
            .SortByDescending(review => review.CreatedAt)
            .Skip(skip)
            .Limit(pageSize)
            .Project(ReviewProjection)
            .ToListAsync(cancellationToken);

        return new PagedResult<ReviewDto>(items, pageNumber, pageSize, totalCount);
    }
}
