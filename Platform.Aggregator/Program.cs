using System.Text.Json;
using System.Text.Json.Serialization;
using Platform.Aggregator.Aggregation;
using Platform.Aggregator.Clients;
using Platform.ServiceDefaults;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddServiceDiscovery();

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.PropertyNameCaseInsensitive = true;
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

builder.Services.AddHttpClient<OrdersApiClient>(client =>
{
    client.BaseAddress = new Uri("https+http://orders-api");
})
.AddHttpMessageHandler<CorrelationIdDelegatingHandler>()
.AddServiceDiscovery();

builder.Services.AddHttpClient<CatalogApiClient>(client =>
{
    client.BaseAddress = new Uri("https+http://catalog-api");
})
.AddHttpMessageHandler<CorrelationIdDelegatingHandler>()
.AddServiceDiscovery();

builder.Services.AddHttpClient<ReviewsApiClient>(client =>
{
    client.BaseAddress = new Uri("https+http://reviews-api");
})
.AddHttpMessageHandler<CorrelationIdDelegatingHandler>()
.AddServiceDiscovery();

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
