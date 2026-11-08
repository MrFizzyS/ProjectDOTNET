namespace Platform.Aggregator.Clients;

public sealed record ServiceCall<T>(T? Data, string? Warning);
