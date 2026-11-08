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
                    .Enrich.WithProperty(
                        "ServiceName",
                        environment.ApplicationName)
                    .Enrich.WithProperty(
                        "Environment",
                        environment.EnvironmentName)
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
            .ConfigureResource(resource =>
                resource.AddService(environment.ApplicationName))
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddSqlClientInstrumentation()
                .AddSource(BusinessTelemetry.Source.Name)
                .AddSource("MongoDB.Driver"))
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation());

        if (!string.IsNullOrWhiteSpace(
                builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]))
        {
            builder.Services.AddOpenTelemetry()
                .UseOtlpExporter();
        }

        builder.Services.AddHealthChecks();
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddTransient<CorrelationIdDelegatingHandler>();

        return builder;
    }

    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        app.MapHealthChecks("/health");
        app.MapHealthChecks("/alive", new HealthCheckOptions
        {
            Predicate = _ => false
        });

        return app;
    }
}