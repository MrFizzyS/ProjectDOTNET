using System.Diagnostics;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.Logging;

namespace Platform.ServiceDefaults.Rpc;

public sealed class GrpcLoggingInterceptor(ILogger<GrpcLoggingInterceptor> logger) : Interceptor
{
    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request,
        ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation)
    {
        var start = Stopwatch.GetTimestamp();
        try
        {
            var response = await continuation(request, context);
            Log(context, StatusCode.OK, start, null);
            return response;
        }
        catch (RpcException ex)
        {
            Log(context, ex.StatusCode, start, ex);
            throw;
        }
        catch (Exception ex)
        {
            Log(context, StatusCode.Internal, start, ex);
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error."));
        }
    }

    private void Log(ServerCallContext context, StatusCode status, long start, Exception? exception)
    {
        var level = status switch
        {
            StatusCode.OK => LogLevel.Information,
            StatusCode.Internal or StatusCode.Unknown => LogLevel.Error,
            _ => LogLevel.Warning
        };

        logger.Log(
            level,
            exception,
            "gRPC {GrpcMethod} from {GrpcPeer} finished with {GrpcStatus} in {ElapsedMs} ms",
            context.Method,
            context.Peer,
            status,
            Stopwatch.GetElapsedTime(start).TotalMilliseconds);
    }
}