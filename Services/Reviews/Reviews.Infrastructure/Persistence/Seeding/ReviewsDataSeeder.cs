using MongoDB.Driver;
using Reviews.Domain.Entities;
using Reviews.Domain.ValueObjects;

namespace Reviews.Infrastructure.Persistence.Seeding;

public sealed class ReviewsDataSeeder(MongoDbContext context) : IDataSeeder
{
    private static readonly Review[] SeedReviews =
    [
        new Review(
            productId: "22222222-2222-4222-8222-222222222222",
            author: new Author("11111111-1111-4111-8111-111111111111", "ada_rolls"),
            rating: new ReviewRating(5),
            body: "A vivid adventure with memorable encounters.",
            productName: "The Ember Crown: Adventure",
            orderId: "33333333-3333-4333-8333-333333333333",
            purchaseStatusSnapshot: "Completed",
            aspects: new ReviewAspects(5, 5, 4))
    ];

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var missingReviews = new List<Review>();

        foreach (var review in SeedReviews)
        {
            var exists = await context.Reviews
                .Find(existing => existing.ProductId == review.ProductId
                                  && existing.Author.CustomerId == review.Author.CustomerId)
                .AnyAsync(cancellationToken);

            if (!exists)
            {
                missingReviews.Add(review);
            }
        }

        if (missingReviews.Count > 0)
        {
            await context.Reviews.InsertManyAsync(
                missingReviews,
                new InsertManyOptions { IsOrdered = false },
                cancellationToken);
        }
    }
}
