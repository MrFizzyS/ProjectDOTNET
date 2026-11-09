using System.Globalization;
using Catalog.Api.Protos;
using Catalog.Bll;
using Catalog.Bll.Contracts;
using Catalog.Bll.Services;
using Catalog.Bll.Validation;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;

namespace Catalog.Api.GrpcServices;

public sealed class CatalogGrpcService(IProductService products) : CatalogGrpc.CatalogGrpcBase
{
    public override async Task<ProductMessage> GetProduct(GetProductRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.ProductId, out var id))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "product_id must be a valid GUID."));
        }

        try
        {
            return ToMessage(await products.GetByIdAsync(id, context.CancellationToken));
        }
        catch (EntityNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
    }

    public override async Task<ListProductsReply> ListProducts(ListProductsRequest request, ServerCallContext context)
    {
        var query = new ProductQueryDto
        {
            Page = request.Page > 0 ? request.Page : 1,
            PageSize = request.PageSize > 0 ? request.PageSize : 20,
            Search = string.IsNullOrWhiteSpace(request.Search) ? null : request.Search
        };

        try
        {
            var page = await products.GetAllAsync(query, context.CancellationToken);
            var reply = new ListProductsReply { Page = page.Page, PageSize = page.PageSize, TotalCount = page.TotalCount };
            reply.Items.AddRange(page.Items.Select(ToMessage));
            return reply;
        }
        catch (RequestValidationException ex)
        {
            throw new RpcException(new Status(
                StatusCode.InvalidArgument,
                string.Join("; ", ex.Errors.SelectMany(e => e.Value))));
        }
    }

    private static ProductMessage ToMessage(ProductDto p)
    {
        var message = new ProductMessage
        {
            ProductId = p.ProductId.ToString(),
            Sku = p.Sku,
            Name = p.Name,
            Description = p.Description,
            ProductType = p.ProductType,
            UnitPrice = p.UnitPrice.ToString(CultureInfo.InvariantCulture),
            StockQuantity = p.StockQuantity,
            IsActive = p.IsActive,
            CreatedAt = Timestamp.FromDateTime(DateTime.SpecifyKind(p.CreatedAt, DateTimeKind.Utc)),
            UpdatedAt = Timestamp.FromDateTime(DateTime.SpecifyKind(p.UpdatedAt, DateTimeKind.Utc))
        };

        if (p.GameSystemId is not null) message.GameSystemId = p.GameSystemId.ToString();
        if (p.GameSystemName is not null) message.GameSystemName = p.GameSystemName;
        return message;
    }
}