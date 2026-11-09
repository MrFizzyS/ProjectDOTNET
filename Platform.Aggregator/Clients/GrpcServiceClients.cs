using System.Globalization;
using Catalog.Api.Protos;
using Orders.Api.Protos;
using Platform.Aggregator.Contracts;
using Reviews.WebApi.Protos;

namespace Platform.Aggregator.Clients;

internal static class GrpcDefaults
{
    public static DateTime Deadline() => DateTime.UtcNow.AddSeconds(3);
}

public sealed class CatalogServiceClient(CatalogGrpc.CatalogGrpcClient grpc)
{
    public async Task<CatalogProductDto> GetProductAsync(Guid productId, CancellationToken cancellationToken)
    {
        var p = await grpc.GetProductAsync(
            new GetProductRequest { ProductId = productId.ToString() },
            deadline: GrpcDefaults.Deadline(),
            cancellationToken: cancellationToken);

        return new CatalogProductDto
        {
            ProductId = Guid.Parse(p.ProductId),
            GameSystemName = p.HasGameSystemName ? p.GameSystemName : null,
            Sku = p.Sku,
            Name = p.Name,
            Description = p.Description,
            ProductType = p.ProductType,
            UnitPrice = decimal.Parse(p.UnitPrice, CultureInfo.InvariantCulture),
            StockQuantity = p.StockQuantity,
            IsActive = p.IsActive
        };
    }
}

public sealed class OrdersServiceClient(OrdersGrpc.OrdersGrpcClient grpc)
{
    public async Task<IReadOnlyList<PurchasedOrderDto>> GetOrdersByProductAsync(
        Guid productId, CancellationToken cancellationToken)
    {
        var reply = await grpc.ListOrdersByProductAsync(
            new ListOrdersByProductRequest { ProductId = productId.ToString() },
            deadline: GrpcDefaults.Deadline(),
            cancellationToken: cancellationToken);

        var id = productId.ToString();
        return reply.Orders
            .Select(order => (order, line: order.Lines.First(l => l.ProductId == id)))
            .Select(x => new PurchasedOrderDto
            {
                OrderId = Guid.Parse(x.order.OrderId),
                Status = x.order.Status,
                ProductNameSnapshot = x.line.ProductNameSnapshot,
                Quantity = (short)x.line.Quantity,
                UnitPriceSnapshot = decimal.Parse(x.line.UnitPriceSnapshot, CultureInfo.InvariantCulture)
            })
            .ToArray();
    }
}

public sealed class ReviewsServiceClient(ReviewsGrpc.ReviewsGrpcClient grpc)
{
    public async Task<IReadOnlyList<ProductReviewDto>> GetByProductAsync(
        Guid productId, CancellationToken cancellationToken)
    {
        var reply = await grpc.ListReviewsByProductAsync(
            new ListReviewsByProductRequest { ProductId = productId.ToString(), Page = 1, PageSize = 20 },
            deadline: GrpcDefaults.Deadline(),
            cancellationToken: cancellationToken);

        return reply.Items.Select(r => new ProductReviewDto
        {
            Id = r.Id,
            ProductName = r.HasProductName ? r.ProductName : null,
            AuthorHandle = r.AuthorHandle,
            Rating = r.Rating,
            Body = r.Body
        }).ToArray();
    }
}