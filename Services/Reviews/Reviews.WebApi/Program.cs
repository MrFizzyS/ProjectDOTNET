using AutoMapper;
using MediatR;
using Platform.ServiceDefaults;
using Platform.ServiceDefaults.Caching;
using Platform.ServiceDefaults.Rpc;
using Reviews.Application;
using Reviews.Infrastructure;
using Reviews.WebApi;
using Reviews.WebApi.GrpcServices;
using Reviews.WebApi.HealthChecks;
using Reviews.WebApi.Metrics;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.AddRedisDistributedCache("redis",
    configureSettings: s => s.DisableHealthChecks = true,
    configureOptions: RedisDefaults.FailFast);
builder.Services.AddGrpc(options =>
{
    options.Interceptors.Add<GrpcLoggingInterceptor>();
    if (builder.Environment.IsDevelopment())
    {
        options.Interceptors.Add<ChaosInterceptor>();
    }
});
var reviewsConnectionString = builder.Configuration.GetConnectionString("ReviewsDb");
if (!string.IsNullOrWhiteSpace(reviewsConnectionString))
    builder.Configuration["MongoDb:ConnectionString"] = reviewsConnectionString;

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddAutoMapper(_ => { }, typeof(ReviewsMappingProfile));
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ReviewsExceptionHandler>();

builder.Services.AddSingleton<ReviewsMetrics>();
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(MetricsBehavior<,>));

builder.Services.AddReviewsApplication();
builder.Services.AddReviewsInfrastructure(builder.Configuration);
builder.Services.AddReviewsHealthChecks(builder.Configuration);

var app = builder.Build();

app.UseExceptionHandler();
app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();
app.MapGrpcService<ReviewsGrpcService>();
app.MapGrpcHealthChecksService();
app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.MapPost("/_chaos/delay/{ms:int}", (int ms) =>
    {
        ChaosState.DelayMs = ms;
        return Results.Ok(new { delayMs = ms });
    });
}

app.Run();