using System.Globalization;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Orders.Api.Protos;
using Orders.Bll;
using Orders.Domain;

namespace Orders.Api.GrpcServices;

public sealed class OrdersGrpcService(IOrderService orders) : OrdersGrpc.OrdersGrpcBase
{
    public override async Task<OrderMessage> GetOrder(GetOrderRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.OrderId, out var id))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "order_id must be a valid GUID."));
        }

        try
        {
            return ToMessage(await orders.GetAsync(id, context.CancellationToken));
        }
        catch (EntityNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
    }

    public override async Task<ListOrdersByProductReply> ListOrdersByProduct(
        ListOrdersByProductRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.ProductId, out var productId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "product_id must be a valid GUID."));
        }

        var list = await orders.GetByProductAsync(productId, context.CancellationToken);
        var reply = new ListOrdersByProductReply();
        reply.Orders.AddRange(list.Select(ToMessage));
        return reply;
    }

    private static OrderMessage ToMessage(OrderDto o)
    {
        var message = new OrderMessage
        {
            OrderId = o.OrderId.ToString(),
            CustomerId = o.CustomerId.ToString(),
            Status = o.Status
        };

        if (o.PlacedAt is { } placed)
        {
            message.PlacedAt = Timestamp.FromDateTime(DateTime.SpecifyKind(placed, DateTimeKind.Utc));
        }

        message.Lines.AddRange(o.Lines.Select(l => new OrderLineMessage
        {
            ProductId = l.ProductId.ToString(),
            ProductNameSnapshot = l.ProductNameSnapshot,
            Quantity = l.Quantity,
            UnitPriceSnapshot = l.UnitPriceSnapshot.ToString(CultureInfo.InvariantCulture)
        }));
        return message;
    }
}