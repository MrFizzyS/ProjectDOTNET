using AutoMapper;
using Reviews.WebApi;
using Reviews.Application;
using Reviews.Infrastructure;
using Platform.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
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
app.MapDefaultEndpoints();

app.Run();
