using Catalog.Bll.Contracts;
using Catalog.Bll.Services;

namespace Catalog.Api.Metrics;

public sealed class MeteredProductService(IProductService inner, CatalogMetrics metrics) : IProductService
{
    public Task<PagedResultDto<ProductDto>> GetAllAsync(ProductQueryDto query, CancellationToken cancellationToken = default) =>
        metrics.MeasureAsync("list", () => inner.GetAllAsync(query, cancellationToken));

    public Task<ProductDto> GetByIdAsync(Guid productId, CancellationToken cancellationToken = default) =>
        metrics.MeasureAsync("get", () => inner.GetByIdAsync(productId, cancellationToken));

    public async Task<ProductDto> CreateAsync(CreateProductDto request, CancellationToken cancellationToken = default)
    {
        var product = await metrics.MeasureAsync("create", () => inner.CreateAsync(request, cancellationToken));
        metrics.ProductCreated(product.ProductType);
        return product;
    }

    public async Task<ProductDto> UpdateAsync(Guid productId, UpdateProductDto request, CancellationToken cancellationToken = default)
    {
        var product = await metrics.MeasureAsync("update", () => inner.UpdateAsync(productId, request, cancellationToken));
        metrics.ProductUpdated(product.ProductType);
        return product;
    }

    public async Task DeleteAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        await metrics.MeasureAsync("delete", () => inner.DeleteAsync(productId, cancellationToken));
        metrics.ProductDeleted();
    }
}