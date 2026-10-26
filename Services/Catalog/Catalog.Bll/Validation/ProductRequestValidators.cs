using Catalog.Bll.Contracts;
using FluentValidation;

namespace Catalog.Bll.Validation;

public sealed class CreateProductValidator : AbstractValidator<CreateProductDto>
{
    public CreateProductValidator()
    {
        RuleFor(request => request.Sku).Must(value => !string.IsNullOrWhiteSpace(value)).WithMessage("SKU is required.");
        RuleFor(request => request.Name).Must(value => !string.IsNullOrWhiteSpace(value)).WithMessage("Name is required.");
        RuleFor(request => request.ProductType).Must(ProductTypeRules.IsSupported)
            .WithMessage("Product type must be CoreBook, Adventure, Dice, Accessory, or Digital.");
        RuleFor(request => request.UnitPrice).GreaterThan(0);
        RuleFor(request => request.StockQuantity).GreaterThanOrEqualTo(0);
    }
}

public sealed class UpdateProductValidator : AbstractValidator<UpdateProductDto>
{
    public UpdateProductValidator()
    {
        RuleFor(request => request.Sku).Must(value => !string.IsNullOrWhiteSpace(value)).WithMessage("SKU is required.");
        RuleFor(request => request.Name).Must(value => !string.IsNullOrWhiteSpace(value)).WithMessage("Name is required.");
        RuleFor(request => request.ProductType).Must(ProductTypeRules.IsSupported)
            .WithMessage("Product type must be CoreBook, Adventure, Dice, Accessory, or Digital.");
        RuleFor(request => request.UnitPrice).GreaterThan(0);
        RuleFor(request => request.StockQuantity).GreaterThanOrEqualTo(0);
        RuleFor(request => request.RowVersion).Must(ProductTypeRules.IsBase64)
            .WithMessage("RowVersion must be a valid Base64 value.");
    }
}

public sealed class ProductQueryValidator : AbstractValidator<ProductQueryDto>
{
    private static readonly string[] SortFields = ["name", "sku", "price", "createdAt", "productType", "stockQuantity"];

    public ProductQueryValidator()
    {
        RuleFor(query => query.Page).InclusiveBetween(1, 1_000_000);
        RuleFor(query => query.PageSize).InclusiveBetween(1, 100);
        RuleFor(query => query.Search).MaximumLength(100);
        RuleFor(query => query.ProductType)
            .Must(value => value is null || ProductTypeRules.IsSupported(value))
            .WithMessage("ProductType is not supported.");
        RuleFor(query => query.SortBy).Must(value => SortFields.Contains(value, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"SortBy must be one of: {string.Join(", ", SortFields)}.");
        RuleFor(query => query).Must(query => !query.MinPrice.HasValue || !query.MaxPrice.HasValue || query.MinPrice <= query.MaxPrice)
            .WithName("PriceRange").WithMessage("MinPrice cannot exceed MaxPrice.");
    }
}

internal static class ProductTypeRules
{
    public static bool IsSupported(string? value)
        => value is "CoreBook" or "Adventure" or "Dice" or "Accessory" or "Digital";

    public static bool IsBase64(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return false;
        try { _ = Convert.FromBase64String(value); return true; }
        catch (FormatException) { return false; }
    }
}
