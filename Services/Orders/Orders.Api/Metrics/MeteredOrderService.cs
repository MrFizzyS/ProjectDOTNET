using Orders.Bll;

namespace Orders.Api.Metrics;

public sealed class MeteredOrderService(IOrderService inner, OrderMetrics metrics) : IOrderService
{
    public Task<IReadOnlyList<OrderDto>> GetAllAsync(CancellationToken ct) =>
        metrics.MeasureAsync("list", () => inner.GetAllAsync(ct));

    public Task<OrderDto> GetAsync(Guid id, CancellationToken ct) =>
        metrics.MeasureAsync("get", () => inner.GetAsync(id, ct));

    public Task<IReadOnlyList<OrderDto>> GetByProductAsync(Guid productId, CancellationToken ct) =>
        metrics.MeasureAsync("list_by_product", () => inner.GetByProductAsync(productId, ct));

    public async Task<OrderDto> CreateAsync(CreateOrderDto dto, CancellationToken ct)
    {
        var order = await metrics.MeasureAsync("create", () => inner.CreateAsync(dto, ct));
        metrics.OrderCreated(order.Status);
        return order;
    }

    public async Task<OrderDto> ChangeStatusAsync(Guid id, UpdateOrderStatusDto dto, CancellationToken ct)
    {
        var order = await metrics.MeasureAsync("change_status", () => inner.ChangeStatusAsync(id, dto, ct));
        metrics.StatusChanged(order.Status);
        return order;
    }

    public Task DeleteAsync(Guid id, CancellationToken ct) =>
        metrics.MeasureAsync("delete", () => inner.DeleteAsync(id, ct));
}