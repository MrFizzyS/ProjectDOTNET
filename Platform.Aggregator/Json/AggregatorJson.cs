using System.Text.Json;

namespace Platform.Aggregator.Json;

internal static class AggregatorJson
{
    public static readonly JsonSerializerOptions Deserializer = new(JsonSerializerDefaults.Web);
}
