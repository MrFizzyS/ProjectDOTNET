using System.Diagnostics;
using Platform.Aggregator.Clients;
using Platform.Aggregator.Contracts;

namespace Platform.Aggregator.Aggregation;

public sealed class ProductOverviewAggregator(
    OrdersApiClient ordersClient,
    CatalogApiClient catalogClient,
    ReviewsApiClient reviewsClient,
    ILogger<ProductOverviewAggregator> logger)
{
    public async Task<ProductOverviewDto> GetAsync(Guid productId, CancellationToken cancellationToken)
    {
        var catalogCall = CallAsync(
            "catalog-api",
            () => catalogClient.GetProductAsync(productId, cancellationToken),
            cancellationToken);
        var ordersCall = CallAsync(
            "orders-api",
            () => ordersClient.GetOrdersAsync(cancellationToken),
            cancellationToken);
        var reviewsCall = CallAsync(
            "reviews-api",
            () => reviewsClient.GetByProductAsync(productId, cancellationToken),
            cancellationToken);

        await Task.WhenAll(catalogCall, ordersCall, reviewsCall);

        var catalog = await catalogCall;
        var orders = await ordersCall;
        var reviews = await reviewsCall;

        var warnings = new List<string>();
        AddWarning(warnings, catalog.Warning);
        AddWarning(warnings, orders.Warning);
        AddWarning(warnings, reviews.Warning);

        return new ProductOverviewDto
        {
            ProductId = productId,
            Product = catalog.Data,
            Orders = orders.Data?
                .Where(order => order.Lines.Any(line => line.ProductId == productId))
                .Select(order =>
                {
                    var line = order.Lines.First(item => item.ProductId == productId);
                    return new PurchasedOrderDto
                    {
                        OrderId = order.OrderId,
                        Status = order.Status,
                        ProductNameSnapshot = line.ProductNameSnapshot,
                        Quantity = line.Quantity,
                        UnitPriceSnapshot = line.UnitPriceSnapshot
                    };
                })
                .ToArray(),
            Reviews = reviews.Data,
            Warnings = warnings
        };
    }

    private async Task<ServiceCall<T>> CallAsync<T>(
        string serviceName,
        Func<Task<T>> action,
        CancellationToken cancellationToken)
    {
        var start = Stopwatch.GetTimestamp();

        try
        {
            var data = await action();
            logger.LogInformation(
                "{ServiceName} call completed in {ElapsedMilliseconds} ms",
                serviceName,
                Stopwatch.GetElapsedTime(start).TotalMilliseconds);
            return new ServiceCall<T>(data, null);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogWarning(
                exception,
                "{ServiceName} call failed in {ElapsedMilliseconds} ms",
                serviceName,
                Stopwatch.GetElapsedTime(start).TotalMilliseconds);
            return new ServiceCall<T>(default, $"Сервіс {serviceName} недоступний.");
        }
    }

    private static void AddWarning(List<string> warnings, string? warning)
    {
        if (!string.IsNullOrWhiteSpace(warning))
        {
            warnings.Add(warning);
        }
    }
}
