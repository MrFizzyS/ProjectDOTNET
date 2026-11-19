using Microsoft.Extensions.Diagnostics.HealthChecks;
using Platform.ServiceDefaults;

namespace Orders.Api.HealthChecks;

public static class OrdersHealthChecks
{
    public static IServiceCollection AddOrdersHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHealthChecks()
            .AddSqlServer(
                connectionString: configuration.GetConnectionString("RpgOrdersDb")!,
                name: "orders-db",
                failureStatus: HealthStatus.Unhealthy,
                tags: ["database", "sql", "ready"],
                timeout: TimeSpan.FromSeconds(5))
            .AddRedisCacheHealthCheck(configuration);

        services.AddGrpcHealthChecks(o => o.Services.MapService("", r => r.Tags.Contains("database")));
        return services;
    }
}