using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Reviews.Domain.Entities;
using Reviews.Infrastructure.Persistence.Seeding;

namespace Reviews.Infrastructure.Persistence;

public sealed class ReviewsMongoDbStartupService(
    MongoDbContext context,
    IDataSeeder dataSeeder,
    ILogger<ReviewsMongoDbStartupService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var indexModels = new CreateIndexModel<Review>[]
        {
            new(
                Builders<Review>.IndexKeys
                    .Ascending(review => review.ProductId)
                    .Descending(review => review.CreatedAt),
                new CreateIndexOptions { Name = "IX_Reviews_Product_CreatedAt" }),
            new(
                Builders<Review>.IndexKeys.Text("productName").Text("body"),
                new CreateIndexOptions { Name = "IX_Reviews_ProductName_Body_Text" }),
            new(
                Builders<Review>.IndexKeys
                    .Ascending("author.customerId")
                    .Ascending(review => review.ProductId),
                new CreateIndexOptions
                {
                    Name = "UX_Reviews_AuthorCustomerId_ProductId",
                    Unique = true
                })
        };

        await context.Reviews.Indexes.CreateManyAsync(indexModels, cancellationToken: cancellationToken);
        logger.LogInformation("Review MongoDB indexes are ready.");

        await dataSeeder.SeedAsync(cancellationToken);
        logger.LogInformation("Review MongoDB seed data is ready.");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
