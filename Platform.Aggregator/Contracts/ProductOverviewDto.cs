namespace Platform.Aggregator.Contracts;

public sealed class ProductOverviewDto
{
    public Guid ProductId { get; init; }

    public CatalogProductDto? Product { get; init; }

    public IReadOnlyList<PurchasedOrderDto>? Orders { get; init; }

    public IReadOnlyList<ProductReviewDto>? Reviews { get; init; }

    public IReadOnlyList<string> Warnings { get; init; } = [];
}

public sealed class CatalogProductDto
{
    public Guid ProductId { get; set; }

    public string? GameSystemName { get; set; }

    public string Sku { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string ProductType { get; set; } = string.Empty;

    public decimal UnitPrice { get; set; }

    public int StockQuantity { get; set; }

    public bool IsActive { get; set; }
}

public sealed class OrderLineDto
{
    public Guid ProductId { get; set; }

    public string ProductNameSnapshot { get; set; } = string.Empty;

    public short Quantity { get; set; }

    public decimal UnitPriceSnapshot { get; set; }
}

public sealed class OrderDto
{
    public Guid OrderId { get; set; }

    public string Status { get; set; } = string.Empty;

    public IReadOnlyList<OrderLineDto> Lines { get; set; } = [];
}

public sealed class PurchasedOrderDto
{
    public Guid OrderId { get; init; }

    public string Status { get; init; } = string.Empty;

    public string ProductNameSnapshot { get; init; } = string.Empty;

    public short Quantity { get; init; }

    public decimal UnitPriceSnapshot { get; init; }
}

public sealed class ReviewPageDto
{
    public IReadOnlyList<ProductReviewDto> Items { get; set; } = [];
}

public sealed class ProductReviewDto
{
    public string Id { get; set; } = string.Empty;

    public string? ProductName { get; set; }

    public string AuthorHandle { get; set; } = string.Empty;

    public int Rating { get; set; }

    public string Body { get; set; } = string.Empty;
}
