using System.Diagnostics;
using Microsoft.AspNetCore.Http;

namespace Platform.ServiceDefaults;

public sealed class CorrelationIdDelegatingHandler(
    IHttpContextAccessor httpContextAccessor) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (!request.Headers.Contains(
                CorrelationIdMiddleware.HeaderName))
        {
            var correlationId =
                httpContextAccessor.HttpContext?
                    .Items[CorrelationIdMiddleware.ContextItemKey] as string
                ?? Activity.Current?.TraceId.ToString()
                ?? Guid.NewGuid().ToString();

            request.Headers.TryAddWithoutValidation(
                CorrelationIdMiddleware.HeaderName,
                correlationId);
        }

        return base.SendAsync(request, cancellationToken);
    }
}