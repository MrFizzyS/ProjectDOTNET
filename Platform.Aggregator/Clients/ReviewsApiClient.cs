using Platform.Aggregator.Contracts;
using Platform.Aggregator.Json;

namespace Platform.Aggregator.Clients;

public sealed class ReviewsApiClient(HttpClient httpClient)
{
    public async Task<IReadOnlyList<ProductReviewDto>> GetByProductAsync(
        Guid productId,
        CancellationToken cancellationToken)
    {
        var page = await httpClient.GetFromJsonAsync<ReviewPageDto>(
            $"/api/reviews?productId={productId}",
            AggregatorJson.Deserializer,
            cancellationToken);

        return page?.Items ?? [];
    }
}
