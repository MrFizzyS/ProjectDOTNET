using Platform.Aggregator.Contracts;
using Platform.Aggregator.Json;

namespace Platform.Aggregator.Clients;

public sealed class CatalogApiClient(HttpClient httpClient)
{
    public async Task<CatalogProductDto> GetProductAsync(
        Guid productId,
        CancellationToken cancellationToken)
    {
        var product = await httpClient.GetFromJsonAsync<CatalogProductDto>(
            $"/api/catalog/products/{productId}",
            AggregatorJson.Deserializer,
            cancellationToken);

        return product ?? throw new HttpRequestException("Catalog returned an empty product.");
    }
}
