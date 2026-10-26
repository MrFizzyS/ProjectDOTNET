using AutoMapper;
using Catalog.Bll.Contracts;
using Catalog.Dal.Repositories;
using Catalog.Dal.UnitOfWork;
using Catalog.Domain;
using Catalog.Bll.Specifications;
using Catalog.Bll.Validation;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Bll.Services;

public sealed class ProductService(
    ICatalogUnitOfWork unitOfWork,
    IMapper mapper,
    IValidator<CreateProductDto> createValidator,
    IValidator<UpdateProductDto> updateValidator,
    IValidator<ProductQueryDto> queryValidator) : IProductService
{
    public async Task<PagedResultDto<ProductDto>> GetAllAsync(ProductQueryDto query, CancellationToken cancellationToken = default)
    {
        await ValidateAsync(queryValidator, query, cancellationToken);
        var specification = new ProductCatalogSpecification(query);
        var totalCount = await unitOfWork.Products.CountAsync(specification, cancellationToken);
        var products = await unitOfWork.Products.ListAsync(specification, cancellationToken);
        var totalPages = (int)Math.Ceiling(totalCount / (double)query.PageSize);
        return new PagedResultDto<ProductDto>
        {
            Items = mapper.Map<IReadOnlyList<ProductDto>>(products),
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = totalCount,
            TotalPages = totalPages,
            HasNext = query.Page < totalPages,
            HasPrevious = query.Page > 1 && totalPages > 0
        };
    }

    public async Task<ProductDto> GetByIdAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        var product = await unitOfWork.Products.GetWithCatalogAsync(productId, cancellationToken)
            ?? throw new EntityNotFoundException($"Product '{productId}' was not found.");
        return mapper.Map<ProductDto>(product);
    }

    public async Task<ProductDto> CreateAsync(CreateProductDto request, CancellationToken cancellationToken = default)
    {
        await ValidateAsync(createValidator, request, cancellationToken);
        await EnsureGameSystemExistsAsync(request.GameSystemId, cancellationToken);
        var sku = NormalizeSku(request.Sku);
        if (await unitOfWork.Products.SkuExistsAsync(sku, cancellationToken: cancellationToken))
            throw new BusinessConflictException($"A product with SKU '{sku}' already exists.");

        var product = new Product(Guid.NewGuid(), sku, request.Name, request.Description,
            request.ProductType, request.UnitPrice, request.StockQuantity, request.GameSystemId);
        await unitOfWork.Products.AddAsync(product, cancellationToken);
        await SaveNewProductAsync(sku, cancellationToken);
        return mapper.Map<ProductDto>(product);
    }

    public async Task<ProductDto> UpdateAsync(Guid productId, UpdateProductDto request, CancellationToken cancellationToken = default)
    {
        await ValidateAsync(updateValidator, request, cancellationToken);
        var product = await unitOfWork.Products.GetByIdAsync([productId], cancellationToken)
            ?? throw new EntityNotFoundException($"Product '{productId}' was not found.");
        EnsureCurrentVersion(product, request.RowVersion);

        var sku = NormalizeSku(request.Sku);
        await EnsureGameSystemExistsAsync(request.GameSystemId, cancellationToken);
        if (await unitOfWork.Products.SkuExistsAsync(sku, productId, cancellationToken))
            throw new BusinessConflictException($"A product with SKU '{sku}' already exists.");

        product.UpdateDetails(sku, request.Name, request.Description, request.ProductType, request.GameSystemId);
        product.ChangePrice(request.UnitPrice);
        if (request.StockQuantity > product.StockQuantity)
            product.AddStock(request.StockQuantity - product.StockQuantity);
        else if (request.StockQuantity < product.StockQuantity)
            product.RemoveStock(product.StockQuantity - request.StockQuantity);

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new BusinessConflictException("The product changed while you were updating it. Reload it and retry.");
        }
        catch (DbUpdateException)
        {
            if (await unitOfWork.Products.SkuExistsAsync(sku, productId, cancellationToken))
                throw new BusinessConflictException($"A product with SKU '{sku}' already exists.");
            throw;
        }

        return mapper.Map<ProductDto>(product);
    }

    public async Task DeleteAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        var product = await unitOfWork.Products.GetByIdAsync([productId], cancellationToken)
            ?? throw new EntityNotFoundException($"Product '{productId}' was not found.");
        unitOfWork.Products.Remove(product);
        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new BusinessConflictException("The product changed while it was being deleted. Reload and retry.");
        }
    }

    private async Task SaveNewProductAsync(string sku, CancellationToken cancellationToken)
    {
        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            if (await unitOfWork.Products.SkuExistsAsync(sku, cancellationToken: cancellationToken))
                throw new BusinessConflictException($"A product with SKU '{sku}' already exists.");
            throw;
        }
    }

    private static string NormalizeSku(string sku) => sku.Trim().ToUpperInvariant();

    private static async Task ValidateAsync<T>(IValidator<T> validator, T request, CancellationToken cancellationToken)
    {
        var result = await validator.ValidateAsync(request, cancellationToken);
        if (!result.IsValid)
        {
            var errors = result.Errors
                .GroupBy(error => error.PropertyName)
                .ToDictionary(group => group.Key, group => group.Select(error => error.ErrorMessage).Distinct().ToArray());
            throw new RequestValidationException(errors);
        }
    }

    private async Task EnsureGameSystemExistsAsync(Guid? gameSystemId, CancellationToken cancellationToken)
    {
        if (gameSystemId is not null &&
            await unitOfWork.GameSystems.GetByIdAsync([gameSystemId.Value], cancellationToken) is null)
            throw new EntityNotFoundException($"Game system '{gameSystemId}' was not found.");
    }

    private static void EnsureCurrentVersion(Product product, string requestedVersion)
    {
        byte[] suppliedVersion;
        try
        {
            suppliedVersion = Convert.FromBase64String(requestedVersion);
        }
        catch (FormatException)
        {
            throw new DomainException("RowVersion must be a valid Base64 value.");
        }

        if (!product.RowVersion.SequenceEqual(suppliedVersion))
            throw new BusinessConflictException("The product has changed since it was loaded. Reload it and retry.");
    }
}
