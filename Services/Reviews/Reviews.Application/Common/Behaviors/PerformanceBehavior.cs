using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Reviews.Application.Common.Behaviors;

public sealed class PerformanceBehavior<TRequest, TResponse>(
    ILogger<PerformanceBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private static readonly TimeSpan SlowRequestThreshold = TimeSpan.FromMilliseconds(500);

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var response = await next(cancellationToken);
        stopwatch.Stop();

        if (stopwatch.Elapsed >= SlowRequestThreshold)
        {
            logger.LogWarning(
                "Slow request {RequestName} completed in {ElapsedMilliseconds} ms.",
                typeof(TRequest).Name,
                stopwatch.Elapsed.TotalMilliseconds);
        }
        else
        {
            logger.LogDebug(
                "Request {RequestName} completed in {ElapsedMilliseconds} ms.",
                typeof(TRequest).Name,
                stopwatch.Elapsed.TotalMilliseconds);
        }

        return response;
    }
}
