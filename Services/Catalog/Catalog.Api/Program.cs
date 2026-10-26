using Catalog.Bll;
using Catalog.Bll.Contracts;
using Catalog.Bll.Services;
using Catalog.Bll.Validation;
using Catalog.Domain;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog((context, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "Catalog.Api")
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Information));
var connectionString = Environment.GetEnvironmentVariable("CATALOG_DB_CONNECTION")
    ?? builder.Configuration.GetConnectionString("CatalogDb")
    ?? throw new InvalidOperationException("ConnectionStrings:CatalogDb is missing.");
builder.Services.AddCatalogBll(connectionString);
builder.Services.AddControllers().ConfigureApiBehaviorOptions(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var problem = new ValidationProblemDetails(context.ModelState)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Request validation failed."
        };
        return new BadRequestObjectResult(problem);
    };
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<CatalogExceptionHandler>();

var app = builder.Build();
if (app.Configuration.GetValue("Database:InitializeOnStartup", true))
{
    await using var scope = app.Services.CreateAsyncScope();
    await scope.ServiceProvider.GetRequiredService<ICatalogStartupInitializer>().InitializeAsync();
}
app.UseExceptionHandler();
app.UseSerilogRequestLogging();
app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();
app.Run();

public sealed class CatalogExceptionHandler(IProblemDetailsService problemDetails, ILogger<CatalogExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is RequestValidationException validationException)
        {
            logger.LogInformation(exception, "Request validation failed for {Path}", context.Request.Path);
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return await problemDetails.TryWriteAsync(new ProblemDetailsContext
            {
                HttpContext = context,
                ProblemDetails = new ValidationProblemDetails(validationException.Errors.ToDictionary(
                    error => error.Key, error => error.Value))
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Request validation failed.",
                    Instance = context.Request.Path
                },
                Exception = exception
            });
        }

        var (status, title, detail) = exception switch
        {
            EntityNotFoundException e => (StatusCodes.Status404NotFound, "Resource not found", e.Message),
            BusinessConflictException e => (StatusCodes.Status409Conflict, "Business conflict", e.Message),
            DomainException e => (StatusCodes.Status409Conflict, "Domain conflict", e.Message),
            _ => (StatusCodes.Status500InternalServerError, "Unexpected server error", "An unexpected error occurred.")
        };

        if (status >= StatusCodes.Status500InternalServerError)
            logger.LogError(exception, "Unhandled Catalog API error for {Path}", context.Request.Path);
        else
            logger.LogInformation(exception, "Catalog API request rejected with {StatusCode}", status);

        context.Response.StatusCode = status;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = new ProblemDetails
            {
                Status = status,
                Title = title,
                Detail = detail,
                Instance = context.Request.Path
            },
            Exception = exception
        });
    }
}
