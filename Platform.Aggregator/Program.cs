using System.Text.Json;
using System.Text.Json.Serialization;
using Catalog.Api.Protos;
using Grpc.Health.V1;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Orders.Api.Protos;
using Platform.Aggregator.Aggregation;
using Platform.Aggregator.Clients;
using Platform.Aggregator.HealthChecks;
using Platform.Aggregator.Metrics;
using Platform.Aggregator.Resilience;
using Platform.ServiceDefaults;
using Platform.ServiceDefaults.Caching;
using Reviews.WebApi.Protos;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddServiceDiscovery();
builder.AddRedisDistributedCache("redis",
    configureSettings: s => s.DisableHealthChecks = true,   
    configureOptions: RedisDefaults.FailFast);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.PropertyNameCaseInsensitive = true;
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

builder.Services.AddGrpcClient<CatalogGrpc.CatalogGrpcClient>(o => o.Address = new Uri("https://_grpc.catalog-api"))
    .ConfigureChannel(c => c.ServiceConfig = ResilienceProfiles.Catalog.ToGrpcServiceConfig())
    .AddProfileResilience(ResilienceProfiles.Catalog)
    .AddHttpMessageHandler<CorrelationIdDelegatingHandler>()
    .AddServiceDiscovery();

builder.Services.AddGrpcClient<OrdersGrpc.OrdersGrpcClient>(o => o.Address = new Uri("https://_grpc.orders-api"))
    .ConfigureChannel(c => c.ServiceConfig = ResilienceProfiles.Orders.ToGrpcServiceConfig())
    .AddProfileResilience(ResilienceProfiles.Orders)
    .AddHttpMessageHandler<CorrelationIdDelegatingHandler>()
    .AddServiceDiscovery();

builder.Services.AddGrpcClient<ReviewsGrpc.ReviewsGrpcClient>(o => o.Address = new Uri("https://_grpc.reviews-api"))
    .ConfigureChannel(c => c.ServiceConfig = ResilienceProfiles.Reviews.ToGrpcServiceConfig())
    .AddProfileResilience(ResilienceProfiles.Reviews)
    .AddHttpMessageHandler<CorrelationIdDelegatingHandler>()
    .AddServiceDiscovery();

foreach (var service in new[] { "catalog-api", "orders-api", "reviews-api" })
{
    builder.Services.AddGrpcClient<Health.HealthClient>($"health-{service}", o => o.Address = new Uri($"https://_grpc.{service}"))
        .RemoveAllResilienceHandlers()
        .AddServiceDiscovery();
}

var readyTags = new[] { "grpc", "downstream", "ready" };
builder.Services.AddHealthChecks()
    .AddRedisCacheHealthCheck(builder.Configuration)
    .AddTypeActivatedCheck<GrpcDependencyHealthCheck>("catalog-api", HealthStatus.Unhealthy, readyTags, "catalog-api")
    .AddTypeActivatedCheck<GrpcDependencyHealthCheck>("orders-api", HealthStatus.Degraded, readyTags, "orders-api")
    .AddTypeActivatedCheck<GrpcDependencyHealthCheck>("reviews-api", HealthStatus.Degraded, readyTags, "reviews-api");

builder.Services.AddSingleton<AggregatorMetrics>();
builder.Services.AddScoped<CatalogServiceClient>();
builder.Services.AddScoped<OrdersServiceClient>();
builder.Services.AddScoped<ReviewsServiceClient>();
builder.Services.AddSingleton<ProductOverviewCache>();
builder.Services.AddScoped<ProductOverviewAggregator>();

var app = builder.Build();

ResilienceLog.Factory = app.Services.GetRequiredService<ILoggerFactory>();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseSerilogRequestLogging();

app.MapGet("/", () => Results.Ok(new { service = "aggregator" }));

app.MapGet("/products/{productId:guid}", async (
    Guid productId,
    ProductOverviewAggregator aggregator,
    CancellationToken cancellationToken) =>
{
    var overview = await aggregator.GetAsync(productId, cancellationToken);
    return Results.Ok(overview);
});

app.MapDefaultEndpoints();

app.Run();