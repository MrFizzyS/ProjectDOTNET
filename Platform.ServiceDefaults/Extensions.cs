using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;
using Platform.ServiceDefaults.Caching;

namespace Platform.ServiceDefaults;

public static class Extensions
{
    public static IHostApplicationBuilder AddServiceDefaults(
        this IHostApplicationBuilder builder)
    {
        var environment = builder.Environment;

        builder.Services.AddSerilog(
            (services, loggerConfiguration) =>
            {
                loggerConfiguration
                    .ReadFrom.Configuration(builder.Configuration)
                    .ReadFrom.Services(services)
                    .Enrich.FromLogContext()
                    .Enrich.WithProperty("ServiceName", environment.ApplicationName)
                    .Enrich.WithProperty("Environment", environment.EnvironmentName)
                    .Enrich.WithMachineName()
                    .Enrich.WithThreadId()
                    .MinimumLevel.Is(
                        environment.IsDevelopment()
                            ? LogEventLevel.Debug
                            : LogEventLevel.Information)
                    .WriteTo.Console(new CompactJsonFormatter());
            },
            writeToProviders: true);

        builder.Logging.AddOpenTelemetry(options =>
        {
            options.IncludeFormattedMessage = true;
            options.IncludeScopes = true;
        });

        builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(environment.ApplicationName))
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddSqlClientInstrumentation()
                .AddSource(BusinessTelemetry.Source.Name)
                .AddSource("MongoDB.Driver"))
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddMeter(
                    CacheMetrics.MeterName,
                    "Polly",                  
                    "Orders.Api",
                    "Catalog.Api",
                    "Reviews.Api",
                    "Platform.Aggregator")
                .AddView("*.operation.duration", new ExplicitBucketHistogramConfiguration
                {
                    Boundaries = [5, 10, 25, 50, 100, 250, 500, 1000, 2500, 5000]
                }));

        if (!string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]))
        {
            builder.Services.AddOpenTelemetry().UseOtlpExporter();
        }

        builder.Services.ConfigureHttpClientDefaults(http => http.AddStandardResilienceHandler());

        builder.Services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);

        builder.Services.Configure<HealthCheckPublisherOptions>(o =>
        {
            o.Delay = TimeSpan.FromSeconds(2);
            o.Period = TimeSpan.FromSeconds(10);
        });

        builder.Services.AddHttpContextAccessor();
        builder.Services.AddTransient<CorrelationIdDelegatingHandler>();

        return builder;
    }

    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            ResponseWriter = HealthResponseWriter.WriteAsync
        });
        var live = new HealthCheckOptions { Predicate = r => r.Tags.Contains("live") };
        app.MapHealthChecks("/alive", live);
        app.MapHealthChecks("/health/live", live);
        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = r => r.Tags.Contains("ready"),
            ResponseWriter = HealthResponseWriter.WriteAsync
        });

        return app;
    }
}