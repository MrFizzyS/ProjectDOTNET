using System.Diagnostics;
using Grpc.Core;
using Platform.Aggregator.Clients;
using Platform.Aggregator.Contracts;

namespace Platform.Aggregator.Aggregation;

public sealed class ProductOverviewAggregator(
    OrdersServiceClient ordersClient,
    CatalogServiceClient catalogClient,
    ReviewsServiceClient reviewsClient,
    ProductOverviewCache cache,
    ILogger<ProductOverviewAggregator> logger)
{
    public async Task<ProductOverviewDto> GetAsync(Guid productId, CancellationToken cancellationToken)
    {
        var cached = await cache.TryGetAsync(productId, cancellationToken);
        if (cached is not null)
        {
            return cached;
        }

        var catalogCall = CallAsync("catalog-api", ct => catalogClient.GetProductAsync(productId, ct), cancellationToken);
        var ordersCall = CallAsync("orders-api", ct => ordersClient.GetOrdersByProductAsync(productId, ct), cancellationToken);
        var reviewsCall = CallAsync("reviews-api", ct => reviewsClient.GetByProductAsync(productId, ct), cancellationToken);

        await Task.WhenAll(catalogCall, ordersCall, reviewsCall);

        var catalog = await catalogCall;
        var orders = await ordersCall;
        var reviews = await reviewsCall;

        var warnings = new[] { catalog.Warning, orders.Warning, reviews.Warning }
            .Where(w => !string.IsNullOrWhiteSpace(w))
            .Select(w => w!)
            .ToList();

        var overview = new ProductOverviewDto
        {
            ProductId = productId,
            Product = catalog.Data,
            Orders = orders.Data,
            Reviews = reviews.Data,
            Warnings = warnings
        };

        if (warnings.Count == 0)
        {
            await cache.SetAsync(productId, overview, cancellationToken);
        }
        else
        {
            logger.LogInformation("Partial response for {ProductId}: {WarningCount} warnings, not cached", productId, warnings.Count);
        }

        return overview;
    }

    private async Task<ServiceCall<T>> CallAsync<T>(
        string serviceName,
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken)
    {
        var start = Stopwatch.GetTimestamp();

        try
        {
            var data = await action(cancellationToken);
            logger.LogInformation(
                "{ServiceName} gRPC call completed in {ElapsedMilliseconds} ms",
                serviceName, Stopwatch.GetElapsedTime(start).TotalMilliseconds);
            return new ServiceCall<T>(data, null);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.Cancelled && cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (RpcException ex)
        {
            logger.LogWarning(
                ex, "{ServiceName} gRPC call failed with {GrpcStatus} in {ElapsedMilliseconds} ms",
                serviceName, ex.StatusCode, Stopwatch.GetElapsedTime(start).TotalMilliseconds);

            var warning = ex.StatusCode switch
            {
                StatusCode.NotFound => $"Сервіс {serviceName}: дані не знайдено.",
                StatusCode.Unavailable or StatusCode.DeadlineExceeded => $"Сервіс {serviceName} недоступний.",
                _ => $"Сервіс {serviceName} повернув помилку ({ex.StatusCode})."
            };
            return new ServiceCall<T>(default, warning);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "{ServiceName} call failed in {ElapsedMilliseconds} ms",
                serviceName, Stopwatch.GetElapsedTime(start).TotalMilliseconds);
            return new ServiceCall<T>(default, $"Сервіс {serviceName} недоступний.");
        }
    }
}