using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using Reviews.Application.Common.Exceptions;
using Reviews.Domain.Exceptions;
using Reviews.Domain.Entities;

namespace Reviews.WebApi;

public sealed class ReviewsExceptionHandler(
    IProblemDetailsService problemDetails,
    ILogger<ReviewsExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext context,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is ValidationException validationException)
        {
            var errors = validationException.Errors
                .GroupBy(error => error.PropertyName)
                .ToDictionary(group => group.Key, group => group.Select(error => error.ErrorMessage).ToArray());
            logger.LogInformation(exception, "Review request validation failed for {Path}", context.Request.Path);
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return await problemDetails.TryWriteAsync(new ProblemDetailsContext
            {
                HttpContext = context,
                ProblemDetails = new ValidationProblemDetails(errors)
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
            NotFoundException e => (StatusCodes.Status404NotFound, "Resource not found", e.Message),
            ConflictException e => (StatusCodes.Status409Conflict, "Business conflict", e.Message),
            MongoWriteException e when e.WriteError?.Category == ServerErrorCategory.DuplicateKey
                => (StatusCodes.Status409Conflict, "Duplicate resource", "A review with the same unique values already exists."),
            MongoBulkWriteException<Review> e when e.WriteErrors.Any(error => error.Category == ServerErrorCategory.DuplicateKey)
                => (StatusCodes.Status409Conflict, "Duplicate resource", "A review with the same unique values already exists."),
            MongoCommandException e when e.Code is 11000 or 11001 or 12582
                => (StatusCodes.Status409Conflict, "Duplicate resource", "A review with the same unique values already exists."),
            MongoConnectionException
                => (StatusCodes.Status503ServiceUnavailable, "MongoDB unavailable", "The review service cannot connect to its database."),
            DomainException e => (StatusCodes.Status400BadRequest, "Invalid review", e.Message),
            FormatException e => (StatusCodes.Status400BadRequest, "Invalid request", e.Message),
            _ => (StatusCodes.Status500InternalServerError, "Unexpected server error", "An unexpected error occurred.")
        };

        if (status >= StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Unhandled Reviews API error for {Path}", context.Request.Path);
        }
        else
        {
            logger.LogInformation(exception, "Reviews API request failed with {StatusCode}", status);
        }

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
