using Grpc.Core;
using Grpc.Core.Interceptors;

namespace Reviews.WebApi.GrpcServices;

public static class ChaosState
{
    public static volatile int DelayMs;
}

public sealed class ChaosInterceptor : Interceptor
{
    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request, ServerCallContext context, UnaryServerMethod<TRequest, TResponse> continuation)
    {
        var delay = ChaosState.DelayMs;
        if (delay > 0 && !context.Method.StartsWith("/grpc.health", StringComparison.Ordinal))
        {
            await Task.Delay(delay, context.CancellationToken);
        }
        return await continuation(request, context);
    }
}