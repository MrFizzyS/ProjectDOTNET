using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Platform.ServiceDefaults.Caching;

public sealed record TieredCacheEntryOptions(TimeSpan L1Ttl, TimeSpan L2Ttl);

public sealed class TieredCacheOptions
{
    public JsonSerializerOptions Json { get; } = new(JsonSerializerDefaults.Web);
}

public interface ITieredCache
{
    Task<T?> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<T?>> factory,
        TieredCacheEntryOptions options,
        CancellationToken cancellationToken = default) where T : class;

    Task RemoveByPrefixAsync(string prefix, string reason, CancellationToken cancellationToken = default);
}

public sealed class TieredCache(
    IMemoryCache l1,
    IDistributedCache l2,
    IOptions<TieredCacheOptions> options,
    ILogger<TieredCache> logger) : ITieredCache
{
    // Реєстр ключів, записаних цим процесом: потрібен для інвалідації за префіксом.
    private readonly ConcurrentDictionary<string, DateTime> _knownKeys = new();

    private JsonSerializerOptions Json => options.Value.Json;

    public async Task<T?> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<T?>> factory,
        TieredCacheEntryOptions entryOptions,
        CancellationToken cancellationToken = default) where T : class
    {
        var cacheName = key.Split(':', 2)[0];

        if (l1.TryGetValue(key, out T? fromL1) && fromL1 is not null)
        {
            CacheMetrics.Hit("L1", cacheName);
            logger.LogInformation("Cache HIT {CacheLevel} {CacheKey}", "L1", key);
            return fromL1;
        }
        CacheMetrics.Miss("L1", cacheName);

        try
        {
            var bytes = await l2.GetAsync(key, cancellationToken);
            if (bytes is not null)
            {
                var fromL2 = JsonSerializer.Deserialize<T>(bytes, Json);
                if (fromL2 is not null)
                {
                    CacheMetrics.Hit("L2", cacheName);
                    logger.LogInformation("Cache HIT {CacheLevel} {CacheKey}", "L2", key);
                    SetL1(key, fromL2, entryOptions);
                    return fromL2;
                }
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "L2 read failed for {CacheKey}; falling back to the source", key);
        }

        CacheMetrics.Miss("L2", cacheName);
        logger.LogInformation("Cache MISS {CacheKey}; loading from the source", key);

        var created = await factory(cancellationToken);
        if (created is null)
        {
            return null; // 404 та порожні результати не кешуємо
        }

        _knownKeys[key] = DateTime.UtcNow + entryOptions.L2Ttl;
        SetL1(key, created, entryOptions);
        try
        {
            await l2.SetAsync(
                key,
                JsonSerializer.SerializeToUtf8Bytes(created, Json),
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = entryOptions.L2Ttl },
                cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "L2 write failed for {CacheKey}", key);
        }

        return created;
    }

    public async Task RemoveByPrefixAsync(string prefix, string reason, CancellationToken cancellationToken = default)
    {
        var removed = 0;
        var now = DateTime.UtcNow;

        foreach (var (key, expiresAt) in _knownKeys)
        {
            if (expiresAt < now)
            {
                _knownKeys.TryRemove(key, out _);
                continue;
            }

            if (!key.StartsWith(prefix, StringComparison.Ordinal))
            {
                continue;
            }

            l1.Remove(key);
            try
            {
                await l2.RemoveAsync(key, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "L2 remove failed for {CacheKey}", key);
            }

            _knownKeys.TryRemove(key, out _);
            removed++;
        }

        logger.LogInformation(
            "Cache invalidated: prefix {CachePrefix}, {RemovedKeys} keys, reason {InvalidationReason}",
            prefix, removed, reason);
    }

    private void SetL1<T>(string key, T value, TieredCacheEntryOptions o) where T : class =>
        l1.Set(key, value, new MemoryCacheEntryOptions
        {
            Size = 1,
            Priority = CacheItemPriority.Normal,
            AbsoluteExpirationRelativeToNow = o.L1Ttl,
            SlidingExpiration = TimeSpan.FromTicks(o.L1Ttl.Ticks / 2)
        });
}

public static class TieredCacheServiceCollectionExtensions
{
    public static IServiceCollection AddTieredCache(
        this IServiceCollection services,
        Action<TieredCacheOptions>? configure = null,
        long l1SizeLimit = 1024)
    {
        services.AddMemoryCache(options => options.SizeLimit = l1SizeLimit);
        services.AddOptions<TieredCacheOptions>().Configure(o => configure?.Invoke(o));
        services.AddSingleton<ITieredCache, TieredCache>();
        return services;
    }
}