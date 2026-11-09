using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Reviews.Application.Common.Behaviors;
using Reviews.Application.Caching;
using Platform.ServiceDefaults.Caching;

namespace Reviews.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddReviewsApplication(this IServiceCollection services)
    {
        services.AddMediatR(configuration =>
        {
            configuration.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
            configuration.AddOpenBehavior(typeof(LoggingBehavior<,>));
            configuration.AddOpenBehavior(typeof(PerformanceBehavior<,>));
            configuration.AddOpenBehavior(typeof(ValidationBehavior<,>));
            configuration.AddOpenBehavior(typeof(CachingBehavior<,>));
            configuration.AddOpenBehavior(typeof(CacheInvalidationBehavior<,>));
        });
        services.AddTieredCache(options => options.Json.Converters.Add(new ObjectIdJsonConverter()));
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        return services;
    }
}
