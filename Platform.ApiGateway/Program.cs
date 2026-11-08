using Platform.ServiceDefaults;
using Serilog;
using Yarp.ReverseProxy.Model;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddServiceDiscovery();

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddServiceDiscoveryDestinationResolver();

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();

app.UseSerilogRequestLogging(options =>
{
    options.MessageTemplate =
        "HTTP {RequestMethod} {RequestPath} matched {RouteId} responded {StatusCode} in {Elapsed:0.0000} ms";

    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        var routeId = httpContext.Items.TryGetValue("YarpRouteId", out var value)
            ? value?.ToString() ?? "unmatched"
            : "unmatched";

        diagnosticContext.Set("RouteId", routeId);
    };
});

app.Use(async (context, next) =>
{
    await next();

    var proxyFeature = context.Features.Get<IReverseProxyFeature>();

    if (proxyFeature is not null)
    {
        context.Items["YarpRouteId"] = proxyFeature.Route.Config.RouteId;
    }
});

app.MapDefaultEndpoints();
app.MapReverseProxy();

app.Run();