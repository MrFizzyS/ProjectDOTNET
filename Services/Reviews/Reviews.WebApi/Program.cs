using AutoMapper;
using Reviews.WebApi;
using Reviews.Application;
using Reviews.Infrastructure;
using Platform.ServiceDefaults;
using Reviews.WebApi.GrpcServices;
using Platform.ServiceDefaults.Rpc;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.AddRedisDistributedCache("redis");
builder.Services.AddGrpc(options => options.Interceptors.Add<GrpcLoggingInterceptor>());
var reviewsConnectionString = builder.Configuration.GetConnectionString("ReviewsDb");
if (!string.IsNullOrWhiteSpace(reviewsConnectionString))
    builder.Configuration["MongoDb:ConnectionString"] = reviewsConnectionString;

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddAutoMapper(_ => { }, typeof(ReviewsMappingProfile));
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ReviewsExceptionHandler>();
builder.Services.AddReviewsApplication();
builder.Services.AddReviewsInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseExceptionHandler();
app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();
app.MapGrpcService<ReviewsGrpcService>();
app.MapDefaultEndpoints();

app.Run();
