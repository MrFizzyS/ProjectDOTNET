using MediatR;
using Microsoft.Extensions.Logging;

namespace Reviews.Application.Common.Behaviors;

public sealed class LoggingBehavior<TRequest, TResponse>(
    ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;

        logger.LogInformation(
            "Handling request {RequestName}",
            requestName);

        try
        {
            var response = await next(cancellationToken);

            logger.LogInformation(
                "Handled request {RequestName}",
                requestName);

            return response;
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Request {RequestName} failed",
                requestName);

            throw;
        }
    }
}