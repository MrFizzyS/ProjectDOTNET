using System.Diagnostics;
using Grpc.Core;
using Platform.Aggregator.Clients;
using Platform.Aggregator.Contracts;
using Platform.Aggregator.Metrics;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace Platform.Aggregator.Aggregation;

public sealed class ProductOverviewAggregator(
    OrdersServiceClient ordersClient,
    CatalogServiceClient catalogClient,
    ReviewsServiceClient reviewsClient,
    ProductOverviewCache cache,
    AggregatorMetrics metrics,
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
            metrics.PartialResponse();
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
            var ms = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
            metrics.Call(serviceName, "success", ms);
            logger.LogInformation("{ServiceName} gRPC call completed in {ElapsedMilliseconds} ms", serviceName, ms);
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
            var ms = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
            var circuitOpen = ex.Status.DebugException is BrokenCircuitException;
            var timeout = ex.StatusCode == StatusCode.DeadlineExceeded || ex.Status.DebugException is TimeoutRejectedException;

            var result = circuitOpen ? "circuit_open"
                : timeout ? "timeout"
                : ex.StatusCode switch
                {
                    StatusCode.NotFound => "not_found",
                    StatusCode.Unavailable => "unavailable",
                    _ => "error"
                };
            metrics.Call(serviceName, result, ms);

            logger.LogWarning(
                ex, "{ServiceName} gRPC call failed with {GrpcStatus} ({Result}) in {ElapsedMilliseconds} ms",
                serviceName, ex.StatusCode, result, ms);

            var warning = result switch
            {
                "circuit_open" => $"Сервіс {serviceName} тимчасово вимкнено (circuit breaker), повторіть пізніше.",
                "not_found" => $"Сервіс {serviceName}: дані не знайдено.",
                "timeout" or "unavailable" => $"Сервіс {serviceName} недоступний.",
                _ => $"Сервіс {serviceName} повернув помилку ({ex.StatusCode})."
            };
            return new ServiceCall<T>(default, warning);
        }
        catch (Exception ex)
        {
            var ms = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
            metrics.Call(serviceName, "error", ms);
            logger.LogWarning(ex, "{ServiceName} call failed in {ElapsedMilliseconds} ms", serviceName, ms);
            return new ServiceCall<T>(default, $"Сервіс {serviceName} недоступний.");
        }
    }
}