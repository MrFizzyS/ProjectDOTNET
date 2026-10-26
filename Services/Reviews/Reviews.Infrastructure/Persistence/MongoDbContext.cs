using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Reviews.Domain.Entities;

namespace Reviews.Infrastructure.Persistence;

public sealed class MongoDbContext
{
    public IMongoDatabase Database { get; }
    public IMongoCollection<Review> Reviews { get; }

    public MongoDbContext(IMongoClient client, IOptions<MongoDbOptions> options)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(options);

        MongoMappings.Register();
        Database = client.GetDatabase(options.Value.DatabaseName);
        Reviews = Database.GetCollection<Review>(options.Value.ReviewsCollectionName);
    }
}
