using System.Diagnostics;
using System.Diagnostics.Metrics;
using MediatR;
using Reviews.Application.Reviews.Commands.CreateReview;

namespace Reviews.WebApi.Metrics;

public sealed class ReviewsMetrics
{
    public const string MeterName = "Reviews.Api";

    private readonly Counter<long> _created;
    private readonly Histogram<double> _duration;
    private int _inFlight;

    public ReviewsMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MeterName);
        _created = meter.CreateCounter<long>("reviews.created", "{review}", "Number of created reviews");
        _duration = meter.CreateHistogram<double>("reviews.operation.duration", "ms", "Duration of review operations");
        meter.CreateObservableGauge("reviews.operations.in_flight",
            () => Volatile.Read(ref _inFlight), "{operation}", "Review operations being processed right now");
    }

    public void ReviewCreated(int rating) =>
        _created.Add(1, new KeyValuePair<string, object?>("review.rating", rating));

    public void Begin() => Interlocked.Increment(ref _inFlight);
    public void End() => Interlocked.Decrement(ref _inFlight);

    public void Record(string operation, bool success, double ms) =>
        _duration.Record(ms,
            new KeyValuePair<string, object?>("operation", operation),
            new KeyValuePair<string, object?>("status", success ? "success" : "failure"));
}

public sealed class MetricsBehavior<TRequest, TResponse>(ReviewsMetrics metrics)
    : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var operation = typeof(TRequest).Name;   // CreateReviewCommand, GetReviewsListQuery: низька кардинальність
        metrics.Begin();
        var sw = Stopwatch.StartNew();
        try
        {
            var response = await next(cancellationToken);
            metrics.Record(operation, true, sw.Elapsed.TotalMilliseconds);
            if (request is CreateReviewCommand create)
            {
                metrics.ReviewCreated(create.Rating);
            }
            return response;
        }
        catch
        {
            metrics.Record(operation, false, sw.Elapsed.TotalMilliseconds);
            throw;
        }
        finally
        {
            metrics.End();
        }
    }
}