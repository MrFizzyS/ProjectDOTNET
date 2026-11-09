using System.Text.Json;
using System.Text.Json.Serialization;
using MediatR;
using MongoDB.Bson;
using Platform.ServiceDefaults.Caching;

namespace Reviews.Application.Caching;

public interface ICacheableQuery
{
    string CacheKey { get; }
    TieredCacheEntryOptions CacheOptions { get; }
}

public interface ICacheInvalidatingCommand
{
    string InvalidationPrefix { get; }
    string InvalidationReason { get; }
}

public static class ReviewsCacheKeys
{
    public const string Prefix = "reviews:";

    public static readonly TieredCacheEntryOptions ItemTtl = new(TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(5));
    public static readonly TieredCacheEntryOptions ListTtl = new(TimeSpan.FromSeconds(30), TimeSpan.FromMinutes(2));

    public static string Item(string id) => $"{Prefix}item:{id}";
    public static string List(string? productId, int page, int size) => $"{Prefix}list:{productId ?? "all"}:{page}:{size}";
}

public sealed class ObjectIdJsonConverter : JsonConverter<ObjectId>
{
    public override ObjectId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => ObjectId.Parse(reader.GetString()!);

    public override void Write(Utf8JsonWriter writer, ObjectId value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.ToString());
}

public sealed class CachingBehavior<TRequest, TResponse>(ITieredCache cache)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull, ICacheableQuery
    where TResponse : class
{
    public async Task<TResponse> Handle(
        TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var result = await cache.GetOrCreateAsync<TResponse>(
            request.CacheKey,
            async ct => await next(ct),
            request.CacheOptions,
            cancellationToken);

        return result!;
    }
}

public sealed class CacheInvalidationBehavior<TRequest, TResponse>(ITieredCache cache)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull, ICacheInvalidatingCommand
{
    public async Task<TResponse> Handle(
        TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var response = await next(cancellationToken);
        await cache.RemoveByPrefixAsync(request.InvalidationPrefix, request.InvalidationReason, cancellationToken);
        return response;
    }
}