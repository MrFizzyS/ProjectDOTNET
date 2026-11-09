using System.Text.Json;
using System.Text.Json.Serialization;
using Catalog.Api.Protos;
using Orders.Api.Protos;
using Platform.Aggregator.Aggregation;
using Platform.Aggregator.Clients;
using Platform.ServiceDefaults;
using Reviews.WebApi.Protos;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddServiceDiscovery();
builder.AddRedisDistributedCache("redis");

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.PropertyNameCaseInsensitive = true;
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

builder.Services.AddGrpcClient<CatalogGrpc.CatalogGrpcClient>(o => o.Address = new Uri("https://_grpc.catalog-api"))
    .AddHttpMessageHandler<CorrelationIdDelegatingHandler>()
    .AddServiceDiscovery();

builder.Services.AddGrpcClient<OrdersGrpc.OrdersGrpcClient>(o => o.Address = new Uri("https://_grpc.orders-api"))
    .AddHttpMessageHandler<CorrelationIdDelegatingHandler>()
    .AddServiceDiscovery();

builder.Services.AddGrpcClient<ReviewsGrpc.ReviewsGrpcClient>(o => o.Address = new Uri("https://_grpc.reviews-api"))
    .AddHttpMessageHandler<CorrelationIdDelegatingHandler>()
    .AddServiceDiscovery();

builder.Services.AddScoped<CatalogServiceClient>();
builder.Services.AddScoped<OrdersServiceClient>();
builder.Services.AddScoped<ReviewsServiceClient>();
builder.Services.AddSingleton<ProductOverviewCache>();
builder.Services.AddScoped<ProductOverviewAggregator>();

var app = builder.Build();

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