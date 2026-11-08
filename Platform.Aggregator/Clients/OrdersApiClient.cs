using Platform.Aggregator.Contracts;
using Platform.Aggregator.Json;

namespace Platform.Aggregator.Clients;

public sealed class OrdersApiClient(HttpClient httpClient)
{
    public async Task<IReadOnlyList<OrderDto>> GetOrdersAsync(CancellationToken cancellationToken)
    {
        var orders = await httpClient.GetFromJsonAsync<List<OrderDto>>(
            "/api/orders",
            AggregatorJson.Deserializer,
            cancellationToken);

        return orders ?? [];
    }
}
