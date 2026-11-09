using Platform.ServiceDefaults.Caching;

namespace Orders.Bll.Caching;

public static class OrdersCacheKeys
{
    public const string Prefix = "orders:";
    public const string List = "orders:list";
    public static string Order(Guid id) => $"orders:order:{id}";
    public static string ByProduct(Guid productId) => $"orders:by-product:{productId}";
}

public sealed class CachedOrderService(IOrderService inner, ITieredCache cache) : IOrderService
{
    private static readonly TieredCacheEntryOptions ItemTtl = new(TimeSpan.FromSeconds(30), TimeSpan.FromMinutes(2));
    private static readonly TieredCacheEntryOptions ListTtl = new(TimeSpan.FromSeconds(15), TimeSpan.FromMinutes(1));

    public async Task<IReadOnlyList<OrderDto>> GetAllAsync(CancellationToken ct)
        => (await cache.GetOrCreateAsync<IReadOnlyList<OrderDto>>(OrdersCacheKeys.List, c => inner.GetAllAsync(c), ListTtl, ct))!;

    public async Task<OrderDto> GetAsync(Guid id, CancellationToken ct)
        => (await cache.GetOrCreateAsync<OrderDto>(OrdersCacheKeys.Order(id), c => inner.GetAsync(id, c), ItemTtl, ct))!;

    public async Task<IReadOnlyList<OrderDto>> GetByProductAsync(Guid productId, CancellationToken ct)
        => (await cache.GetOrCreateAsync<IReadOnlyList<OrderDto>>(OrdersCacheKeys.ByProduct(productId), c => inner.GetByProductAsync(productId, c), ListTtl, ct))!;

    public async Task<OrderDto> CreateAsync(CreateOrderDto dto, CancellationToken ct)
    {
        var result = await inner.CreateAsync(dto, ct);
        await cache.RemoveByPrefixAsync(OrdersCacheKeys.Prefix, "order created", ct);
        return result;
    }

    public async Task<OrderDto> ChangeStatusAsync(Guid id, UpdateOrderStatusDto dto, CancellationToken ct)
    {
        var result = await inner.ChangeStatusAsync(id, dto, ct);
        await cache.RemoveByPrefixAsync(OrdersCacheKeys.Prefix, "order status changed", ct);
        return result;
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        await inner.DeleteAsync(id, ct);
        await cache.RemoveByPrefixAsync(OrdersCacheKeys.Prefix, "order deleted", ct);
    }
}