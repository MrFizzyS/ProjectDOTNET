using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Reviews.Application.Common.Behaviors;

public sealed class ValidationBehavior<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>> validators,
    ILogger<ValidationBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var failures = new List<FluentValidation.Results.ValidationFailure>();

        foreach (var validator in validators)
        {
            var result = await validator.ValidateAsync(request, cancellationToken);
            failures.AddRange(result.Errors.Where(failure => failure is not null));
        }

        if (failures.Count > 0)
        {
            logger.LogInformation(
                "Request {RequestName} rejected with {ValidationErrorCount} validation errors.",
                typeof(TRequest).Name,
                failures.Count);
            throw new ValidationException(failures);
        }

        return await next(cancellationToken);
    }
}
