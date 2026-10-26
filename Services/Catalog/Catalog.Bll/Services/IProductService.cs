using Catalog.Bll.Contracts;

namespace Catalog.Bll.Services;

public interface IProductService
{
    Task<PagedResultDto<ProductDto>> GetAllAsync(ProductQueryDto query, CancellationToken cancellationToken = default);
    Task<ProductDto> GetByIdAsync(Guid productId, CancellationToken cancellationToken = default);
    Task<ProductDto> CreateAsync(CreateProductDto request, CancellationToken cancellationToken = default);
    Task<ProductDto> UpdateAsync(Guid productId, UpdateProductDto request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid productId, CancellationToken cancellationToken = default);
}
