using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Platform.ServiceDefaults;

public static class RedisHealthCheckExtensions
{
    public static IHealthChecksBuilder AddRedisCacheHealthCheck(
        this IHealthChecksBuilder builder, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("redis");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return builder;
        }

        return builder.AddRedis(
            redisConnectionString: connectionString,
            name: "redis-cache",
            failureStatus: HealthStatus.Degraded,
            tags: ["cache", "redis", "ready"],
            timeout: TimeSpan.FromSeconds(3));
    }
}