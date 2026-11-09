using System.Globalization;
using Catalog.Bll.Contracts;
using Catalog.Bll.Services;
using Platform.ServiceDefaults.Caching;

namespace Catalog.Bll.Caching;

public static class CatalogCacheKeys
{
    public const string ProductPrefix = "catalog:product:";
    public const string ListPrefix = "catalog:products:list:";

    public static string Product(Guid id) => $"{ProductPrefix}{id}";

    public static string List(ProductQueryDto q) => string.Create(CultureInfo.InvariantCulture,
        $"{ListPrefix}{q.Page}:{q.PageSize}:{q.Search?.Trim().ToLowerInvariant()}:{q.ProductType}:{q.MinPrice}:{q.MaxPrice}:{q.OnlyActive}:{q.SortBy}:{q.Descending}");
}

public sealed class CachedProductService(IProductService inner, ITieredCache cache) : IProductService
{
    private static readonly TieredCacheEntryOptions CardTtl = new(TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(5));
    private static readonly TieredCacheEntryOptions ListTtl = new(TimeSpan.FromSeconds(30), TimeSpan.FromMinutes(2));

    public async Task<PagedResultDto<ProductDto>> GetAllAsync(ProductQueryDto query, CancellationToken cancellationToken = default)
        => (await cache.GetOrCreateAsync<PagedResultDto<ProductDto>>(
            CatalogCacheKeys.List(query), ct => inner.GetAllAsync(query, ct), ListTtl, cancellationToken))!;

    public async Task<ProductDto> GetByIdAsync(Guid productId, CancellationToken cancellationToken = default)
        => (await cache.GetOrCreateAsync<ProductDto>(
            CatalogCacheKeys.Product(productId), ct => inner.GetByIdAsync(productId, ct), CardTtl, cancellationToken))!;

    public async Task<ProductDto> CreateAsync(CreateProductDto request, CancellationToken cancellationToken = default)
    {
        var created = await inner.CreateAsync(request, cancellationToken);
        await cache.RemoveByPrefixAsync(CatalogCacheKeys.ListPrefix, "product created", cancellationToken);
        return created;
    }

    public async Task<ProductDto> UpdateAsync(Guid productId, UpdateProductDto request, CancellationToken cancellationToken = default)
    {
        var updated = await inner.UpdateAsync(productId, request, cancellationToken);
        await cache.RemoveByPrefixAsync(CatalogCacheKeys.Product(productId), "product updated", cancellationToken);
        await cache.RemoveByPrefixAsync(CatalogCacheKeys.ListPrefix, "product updated", cancellationToken);
        return updated;
    }

    public async Task DeleteAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        await inner.DeleteAsync(productId, cancellationToken);
        await cache.RemoveByPrefixAsync(CatalogCacheKeys.Product(productId), "product deleted", cancellationToken);
        await cache.RemoveByPrefixAsync(CatalogCacheKeys.ListPrefix, "product deleted", cancellationToken);
    }
}