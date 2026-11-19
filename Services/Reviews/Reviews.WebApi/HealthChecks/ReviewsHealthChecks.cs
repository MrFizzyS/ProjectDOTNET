using Microsoft.Extensions.Diagnostics.HealthChecks;
using MongoDB.Bson;
using MongoDB.Driver;
using Platform.ServiceDefaults;

namespace Reviews.WebApi.HealthChecks;

public sealed class MongoPingHealthCheck(IMongoClient client) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await client.GetDatabase("admin").RunCommandAsync<BsonDocument>(
                new BsonDocumentCommand<BsonDocument>(new BsonDocument("ping", 1)),
                cancellationToken: cancellationToken);
            return HealthCheckResult.Healthy("MongoDB ping OK");
        }
        catch (Exception ex)
        {
            return new HealthCheckResult(context.Registration.FailureStatus, "MongoDB ping failed", ex);
        }
    }
}

public static class ReviewsHealthChecks
{
    public static IServiceCollection AddReviewsHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHealthChecks()
            .AddCheck<MongoPingHealthCheck>(
                "reviews-db",
                failureStatus: HealthStatus.Unhealthy,
                tags: ["database", "mongodb", "ready"],
                timeout: TimeSpan.FromSeconds(5))
            .AddRedisCacheHealthCheck(configuration);

        services.AddGrpcHealthChecks(o => o.Services.MapService("", r => r.Tags.Contains("database")));
        return services;
    }
}