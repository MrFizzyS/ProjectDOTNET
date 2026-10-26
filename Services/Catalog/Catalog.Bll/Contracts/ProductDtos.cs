using System.ComponentModel.DataAnnotations;

namespace Catalog.Bll.Contracts;

public sealed class ProductDto
{
    public Guid ProductId { get; set; }
    public Guid? GameSystemId { get; set; }
    public string? GameSystemName { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ProductType { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int StockQuantity { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string RowVersion { get; set; } = string.Empty;
}

public sealed class CreateProductDto
{
    [Required, StringLength(40, MinimumLength = 1)]
    public string Sku { get; set; } = string.Empty;

    [Required, StringLength(200, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000)]
    public string Description { get; set; } = string.Empty;

    [Required, RegularExpression("^(CoreBook|Adventure|Dice|Accessory|Digital)$")]
    public string ProductType { get; set; } = string.Empty;

    [Range(typeof(decimal), "0.01", "9999999999999999.99", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]
    public decimal UnitPrice { get; set; }

    [Range(0, int.MaxValue)]
    public int StockQuantity { get; set; }

    public Guid? GameSystemId { get; set; }
}

public sealed class UpdateProductDto
{
    [Required, StringLength(40, MinimumLength = 1)]
    public string Sku { get; set; } = string.Empty;

    [Required, StringLength(200, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000)]
    public string Description { get; set; } = string.Empty;

    [Required, RegularExpression("^(CoreBook|Adventure|Dice|Accessory|Digital)$")]
    public string ProductType { get; set; } = string.Empty;

    [Range(typeof(decimal), "0.01", "9999999999999999.99", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]
    public decimal UnitPrice { get; set; }

    [Range(0, int.MaxValue)]
    public int StockQuantity { get; set; }

    public Guid? GameSystemId { get; set; }

    [Required]
    public string RowVersion { get; set; } = string.Empty;
}

public sealed class ProductQueryDto
{
    [Range(1, 1_000_000)]
    public int Page { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 20;

    [StringLength(100)]
    public string? Search { get; set; }

    [RegularExpression("^(CoreBook|Adventure|Dice|Accessory|Digital)$")]
    public string? ProductType { get; set; }

    [Range(typeof(decimal), "0.01", "9999999999999999.99", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]
    public decimal? MinPrice { get; set; }

    [Range(typeof(decimal), "0.01", "9999999999999999.99", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]
    public decimal? MaxPrice { get; set; }

    public bool OnlyActive { get; set; } = true;

    [RegularExpression("^(name|sku|price|createdAt|productType|stockQuantity)$")]
    public string SortBy { get; set; } = "name";

    public bool Descending { get; set; }
}

public sealed class PagedResultDto<T>
{
    public IReadOnlyList<T> Items { get; init; } = [];
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages { get; init; }
    public bool HasNext { get; init; }
    public bool HasPrevious { get; init; }
}
