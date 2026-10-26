namespace Reviews.Infrastructure.Persistence;

public sealed class MongoDbOptions
{
    public const string SectionName = "MongoDb";

    public string ConnectionString { get; set; } = "mongodb://localhost:27017/";
    public string DatabaseName { get; set; } = "rpg_content";
    public string ReviewsCollectionName { get; set; } = "reviews";
}
