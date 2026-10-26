using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Reviews.Application.Common.Interfaces;
using Reviews.Domain.Interfaces;
using Reviews.Infrastructure.Persistence;
using Reviews.Infrastructure.Persistence.Seeding;

namespace Reviews.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddReviewsInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        MongoMappings.Register();

        services.AddOptions<MongoDbOptions>()
            .Bind(configuration.GetSection(MongoDbOptions.SectionName))
            .Validate(options => !string.IsNullOrWhiteSpace(options.ConnectionString),
                "MongoDB connection string is required.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.DatabaseName),
                "MongoDB database name is required.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.ReviewsCollectionName),
                "MongoDB reviews collection name is required.")
            .ValidateOnStart();

        services.AddSingleton<IMongoClient>(serviceProvider =>
        {
            var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<MongoDbOptions>>();
            return new MongoClient(options.Value.ConnectionString);
        });
        services.AddSingleton<MongoDbContext>();
        services.AddScoped<IReviewRepository, MongoReviewRepository>();
        services.AddScoped<IReviewQueries, MongoReviewQueries>();
        services.AddSingleton<IDataSeeder, ReviewsDataSeeder>();
        services.AddHostedService<ReviewsMongoDbStartupService>();

        return services;
    }
}
