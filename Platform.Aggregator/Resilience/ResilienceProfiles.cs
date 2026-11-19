using Grpc.Core;
using Grpc.Net.Client.Configuration;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Logging.Abstractions;
using Polly;

namespace Platform.Aggregator.Resilience;

public sealed record ResilienceProfile(
    string Name,
    int HttpRetries, TimeSpan HttpRetryDelay,
    double FailureRatio, int MinimumThroughput, TimeSpan SamplingDuration, TimeSpan BreakDuration,
    TimeSpan AttemptTimeout, TimeSpan TotalTimeout,
    int GrpcMaxAttempts, TimeSpan GrpcInitialBackoff, TimeSpan GrpcMaxBackoff,
    TimeSpan GrpcDeadline);

public static class ResilienceProfiles
{
    public static readonly ResilienceProfile Catalog = new("catalog-api",
        HttpRetries: 5, HttpRetryDelay: TimeSpan.FromMilliseconds(300),
        FailureRatio: 0.3, MinimumThroughput: 5,
        SamplingDuration: TimeSpan.FromSeconds(30), BreakDuration: TimeSpan.FromSeconds(10),
        AttemptTimeout: TimeSpan.FromSeconds(3), TotalTimeout: TimeSpan.FromSeconds(20),
        GrpcMaxAttempts: 4, GrpcInitialBackoff: TimeSpan.FromMilliseconds(200), GrpcMaxBackoff: TimeSpan.FromSeconds(2),
        GrpcDeadline: TimeSpan.FromSeconds(10));

    public static readonly ResilienceProfile Orders = new("orders-api",
        HttpRetries: 3, HttpRetryDelay: TimeSpan.FromMilliseconds(500),
        FailureRatio: 0.5, MinimumThroughput: 5,
        SamplingDuration: TimeSpan.FromSeconds(30), BreakDuration: TimeSpan.FromSeconds(15),
        AttemptTimeout: TimeSpan.FromSeconds(3), TotalTimeout: TimeSpan.FromSeconds(12),
        GrpcMaxAttempts: 3, GrpcInitialBackoff: TimeSpan.FromMilliseconds(300), GrpcMaxBackoff: TimeSpan.FromSeconds(2),
        GrpcDeadline: TimeSpan.FromSeconds(6));

    public static readonly ResilienceProfile Reviews = new("reviews-api",
        HttpRetries: 1, HttpRetryDelay: TimeSpan.FromMilliseconds(300),
        FailureRatio: 0.7, MinimumThroughput: 5,
        SamplingDuration: TimeSpan.FromSeconds(30), BreakDuration: TimeSpan.FromSeconds(20),
        AttemptTimeout: TimeSpan.FromSeconds(2), TotalTimeout: TimeSpan.FromSeconds(5),
        GrpcMaxAttempts: 2, GrpcInitialBackoff: TimeSpan.FromMilliseconds(200), GrpcMaxBackoff: TimeSpan.FromSeconds(1),
        GrpcDeadline: TimeSpan.FromSeconds(3));
}

public static class ResilienceLog
{
    public static ILoggerFactory? Factory { get; set; }

    public static ILogger For(string name) =>
        (Factory ?? NullLoggerFactory.Instance).CreateLogger($"Resilience.{name}");
}

public static class ResilienceExtensions
{
    public static IHttpClientBuilder AddProfileResilience(this IHttpClientBuilder builder, ResilienceProfile p)
    {
        builder.RemoveAllResilienceHandlers();
        builder.AddStandardResilienceHandler(o =>
        {
            o.Retry.MaxRetryAttempts = p.HttpRetries;
            o.Retry.Delay = p.HttpRetryDelay;
            o.Retry.BackoffType = DelayBackoffType.Exponential;
            o.Retry.UseJitter = true;

            o.CircuitBreaker.FailureRatio = p.FailureRatio;
            o.CircuitBreaker.MinimumThroughput = p.MinimumThroughput;
            o.CircuitBreaker.SamplingDuration = p.SamplingDuration; 
            o.CircuitBreaker.BreakDuration = p.BreakDuration;
            o.CircuitBreaker.OnOpened = args =>
            {
                ResilienceLog.For(p.Name).LogError(
                    "Circuit OPENED for {Service}, break {BreakDuration}", p.Name, args.BreakDuration);
                return ValueTask.CompletedTask;
            };
            o.CircuitBreaker.OnHalfOpened = _ =>
            {
                ResilienceLog.For(p.Name).LogInformation("Circuit HALF-OPEN for {Service}", p.Name);
                return ValueTask.CompletedTask;
            };
            o.CircuitBreaker.OnClosed = _ =>
            {
                ResilienceLog.For(p.Name).LogInformation("Circuit CLOSED for {Service}", p.Name);
                return ValueTask.CompletedTask;
            };

            o.AttemptTimeout.Timeout = p.AttemptTimeout;
            o.TotalRequestTimeout.Timeout = p.TotalTimeout;
        });
        return builder;
    }

    public static ServiceConfig ToGrpcServiceConfig(this ResilienceProfile p) => new()
    {
        MethodConfigs =
        {
            new MethodConfig
            {
                Names = { MethodName.Default },
                RetryPolicy = new RetryPolicy
                {
                    MaxAttempts = p.GrpcMaxAttempts,
                    InitialBackoff = p.GrpcInitialBackoff,
                    MaxBackoff = p.GrpcMaxBackoff,
                    BackoffMultiplier = 2,
                    RetryableStatusCodes = { StatusCode.Unavailable }
                }
            }
        }
    };
}