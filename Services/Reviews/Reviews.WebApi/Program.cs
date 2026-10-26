using AutoMapper;
using Reviews.WebApi;
using Reviews.Application;
using Reviews.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

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

app.Run();
