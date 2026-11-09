using System.Diagnostics.Metrics;

namespace Platform.ServiceDefaults.Caching;

public static class CacheMetrics
{
    public const string MeterName = "Platform.Cache";

    private static readonly Meter Meter = new(MeterName, "1.0.0");
    private static readonly Counter<long> Hits = Meter.CreateCounter<long>("cache.hits", description: "Cache hits");
    private static readonly Counter<long> Misses = Meter.CreateCounter<long>("cache.misses", description: "Cache misses");

    public static void Hit(string level, string cache) =>
        Hits.Add(1, new KeyValuePair<string, object?>("level", level), new KeyValuePair<string, object?>("cache", cache));

    public static void Miss(string level, string cache) =>
        Misses.Add(1, new KeyValuePair<string, object?>("level", level), new KeyValuePair<string, object?>("cache", cache));
}