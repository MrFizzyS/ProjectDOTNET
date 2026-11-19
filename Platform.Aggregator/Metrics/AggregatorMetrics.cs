using System.Diagnostics.Metrics;

namespace Platform.Aggregator.Metrics;

public sealed class AggregatorMetrics
{
    public const string MeterName = "Platform.Aggregator";

    private readonly Counter<long> _calls;
    private readonly Counter<long> _partial;
    private readonly Histogram<double> _duration;

    public AggregatorMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MeterName);
        _calls = meter.CreateCounter<long>("aggregator.downstream.calls", "{call}", "Calls from aggregator to downstream services");
        _partial = meter.CreateCounter<long>("aggregator.partial_responses", "{response}", "Responses returned with warnings");
        _duration = meter.CreateHistogram<double>("aggregator.operation.duration", "ms", "Downstream call duration");
    }

    public void Call(string service, string result, double ms)
    {
        _calls.Add(1,
            new KeyValuePair<string, object?>("downstream.service", service),
            new KeyValuePair<string, object?>("result", result));
        _duration.Record(ms,
            new KeyValuePair<string, object?>("downstream.service", service),
            new KeyValuePair<string, object?>("status", result == "success" ? "success" : "failure"));
    }

    public void PartialResponse() => _partial.Add(1);
}