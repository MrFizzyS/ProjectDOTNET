using Microsoft.Extensions.Diagnostics.HealthChecks;
using Platform.ServiceDefaults;

namespace Catalog.Api.HealthChecks;

public static class CatalogHealthChecks
{
    public static IServiceCollection AddCatalogHealthChecks(
        this IServiceCollection services, string connectionString, IConfiguration configuration)
    {
        services.AddHealthChecks()
            .AddSqlServer(
                connectionString: connectionString,
                name: "catalog-db",
                failureStatus: HealthStatus.Unhealthy,
                tags: ["database", "sql", "ready"],
                timeout: TimeSpan.FromSeconds(5))
            .AddRedisCacheHealthCheck(configuration);

        services.AddGrpcHealthChecks(o => o.Services.MapService("", r => r.Tags.Contains("database")));
        return services;
    }
}