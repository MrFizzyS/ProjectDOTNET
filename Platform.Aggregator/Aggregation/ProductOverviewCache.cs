using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Platform.Aggregator.Contracts;
using Platform.Aggregator.Json;
using Platform.ServiceDefaults.Caching;

namespace Platform.Aggregator.Aggregation;

public sealed class ProductOverviewCache(IDistributedCache cache, ILogger<ProductOverviewCache> logger)
{
    private static readonly TimeSpan Ttl = TimeSpan.FromSeconds(45);

    private static string Key(Guid productId) => $"aggregator:product-overview:{productId}";

    public async Task<ProductOverviewDto?> TryGetAsync(Guid productId, CancellationToken cancellationToken)
    {
        try
        {
            var bytes = await cache.GetAsync(Key(productId), cancellationToken);
            if (bytes is not null)
            {
                CacheMetrics.Hit("L2", "aggregator");
                logger.LogInformation("Aggregator cache HIT {CacheKey}", Key(productId));
                return JsonSerializer.Deserialize<ProductOverviewDto>(bytes, AggregatorJson.Deserializer);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Aggregator cache read failed for {CacheKey}", Key(productId));
        }

        CacheMetrics.Miss("L2", "aggregator");
        logger.LogInformation("Aggregator cache MISS {CacheKey}", Key(productId));
        return null;
    }

    public async Task SetAsync(Guid productId, ProductOverviewDto overview, CancellationToken cancellationToken)
    {
        try
        {
            await cache.SetAsync(
                Key(productId),
                JsonSerializer.SerializeToUtf8Bytes(overview, AggregatorJson.Deserializer),
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = Ttl },
                cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Aggregator cache write failed for {CacheKey}", Key(productId));
        }
    }
}