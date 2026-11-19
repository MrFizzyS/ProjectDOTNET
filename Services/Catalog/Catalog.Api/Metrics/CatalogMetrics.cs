using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Catalog.Api.Metrics;

public sealed class CatalogMetrics
{
    public const string MeterName = "Catalog.Api";

    private readonly Counter<long> _created;
    private readonly Counter<long> _updated;
    private readonly Counter<long> _deleted;
    private readonly Histogram<double> _duration;
    private int _inFlight;

    public CatalogMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MeterName);
        _created = meter.CreateCounter<long>("products.created", "{product}", "Number of created products");
        _updated = meter.CreateCounter<long>("products.updated", "{product}", "Number of updated products");
        _deleted = meter.CreateCounter<long>("products.deleted", "{product}", "Number of deleted products");
        _duration = meter.CreateHistogram<double>("catalog.operation.duration", "ms", "Duration of catalog operations");
        meter.CreateObservableGauge("catalog.operations.in_flight",
            () => Volatile.Read(ref _inFlight), "{operation}", "Catalog operations being processed right now");
    }

    public void ProductCreated(string productType) =>
        _created.Add(1, new KeyValuePair<string, object?>("product.type", productType));

    public void ProductUpdated(string productType) =>
        _updated.Add(1, new KeyValuePair<string, object?>("product.type", productType));

    public void ProductDeleted() => _deleted.Add(1);

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