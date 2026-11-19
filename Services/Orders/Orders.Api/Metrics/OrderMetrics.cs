using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Orders.Api.Metrics;

public sealed class OrderMetrics
{
    public const string MeterName = "Orders.Api";

    private readonly Counter<long> _created;
    private readonly Counter<long> _statusChanged;
    private readonly Histogram<double> _duration;
    private int _inFlight;

    public OrderMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MeterName);
        _created = meter.CreateCounter<long>("orders.created", "{order}", "Number of created orders");
        _statusChanged = meter.CreateCounter<long>("orders.status_changed", "{order}", "Number of order status transitions");
        _duration = meter.CreateHistogram<double>("orders.operation.duration", "ms", "Duration of order operations");
        meter.CreateObservableGauge("orders.operations.in_flight",
            () => Volatile.Read(ref _inFlight), "{operation}", "Order operations being processed right now");
    }

    public void OrderCreated(string status) =>
        _created.Add(1, new KeyValuePair<string, object?>("order.status", status));

    public void StatusChanged(string status) =>
        _statusChanged.Add(1, new KeyValuePair<string, object?>("order.status", status));

    public async Task<T> MeasureAsync<T>(string operation, Func<Task<T>> action)
    {
        Interlocked.Increment(ref _inFlight);
        var sw = Stopwatch.StartNew();
        try
        {
            var result = await action();
            Record(operation, true, sw.Elapsed.TotalMilliseconds);
            return result;
        }
        catch
        {
            Record(operation, false, sw.Elapsed.TotalMilliseconds);
            throw;
        }
        finally
        {
            Interlocked.Decrement(ref _inFlight);
        }
    }

    public async Task MeasureAsync(string operation, Func<Task> action) =>
        await MeasureAsync<object?>(operation, async () => { await action(); return null; });

    private void Record(string operation, bool success, double ms) =>
        _duration.Record(ms,
            new KeyValuePair<string, object?>("operation", operation),
            new KeyValuePair<string, object?>("status", success ? "success" : "failure"));
}