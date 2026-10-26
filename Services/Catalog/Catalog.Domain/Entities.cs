namespace Catalog.Domain;

public sealed class GameSystem
{
    private GameSystem() { }
    public GameSystem(Guid id, string name, string publisher, string edition)
    {
        if (id == Guid.Empty) throw new DomainException("Game system id is required.");
        GameSystemId = id;
        Name = Required(name, nameof(name), 100);
        Publisher = Required(publisher, nameof(publisher), 120);
        Edition = Required(edition, nameof(edition), 40);
    }
    public Guid GameSystemId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Publisher { get; private set; } = string.Empty;
    public string Edition { get; private set; } = string.Empty;
    public ICollection<Product> Products { get; private set; } = new List<Product>();
    public ICollection<CatalogCollection> Collections { get; private set; } = new List<CatalogCollection>();
    private static string Required(string value, string field, int max)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new DomainException($"{field} is required.");
        var text = value.Trim();
        if (text.Length > max) throw new DomainException($"{field} cannot exceed {max} characters.");
        return text;
    }
}

public sealed class Product
{
    private Product() { }
    public Product(Guid id, string sku, string name, string description, string productType,
        decimal unitPrice, int stockQuantity, Guid? gameSystemId = null)
    {
        if (id == Guid.Empty) throw new DomainException("Product id is required.");
        ProductId = id;
        Sku = Required(sku, nameof(sku), 40).ToUpperInvariant();
        Name = Required(name, nameof(name), 200);
        Description = (description ?? string.Empty).Trim();
        if (Description.Length > 1000) throw new DomainException("Description cannot exceed 1000 characters.");
        ProductType = ValidateProductType(productType);
        ValidatePrice(unitPrice);
        if (stockQuantity < 0) throw new DomainException("Stock quantity cannot be negative.");
        UnitPrice = unitPrice;
        StockQuantity = stockQuantity;
        GameSystemId = gameSystemId;
        IsActive = true;
        CreatedAt = UpdatedAt = DateTime.UtcNow;
    }
    public Guid ProductId { get; private set; }
    public Guid? GameSystemId { get; private set; }
    public string Sku { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string ProductType { get; private set; } = string.Empty;
    public decimal UnitPrice { get; private set; }
    public int StockQuantity { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public byte[] RowVersion { get; private set; } = [];
    public GameSystem? GameSystem { get; private set; }
    public ProductDetail? Detail { get; private set; }
    public ICollection<ProductImage> Images { get; private set; } = new List<ProductImage>();
    public ICollection<ProductCategory> ProductCategories { get; private set; } = new List<ProductCategory>();
    public ICollection<CollectionItem> CollectionItems { get; private set; } = new List<CollectionItem>();

    public void ChangePrice(decimal price) { ValidatePrice(price); UnitPrice = price; UpdatedAt = DateTime.UtcNow; }
    public void UpdateDetails(string sku, string name, string description, string productType, Guid? gameSystemId)
    {
        Sku = Required(sku, nameof(sku), 40).ToUpperInvariant();
        Name = Required(name, nameof(name), 200);
        Description = (description ?? string.Empty).Trim();
        if (Description.Length > 1000) throw new DomainException("Description cannot exceed 1000 characters.");
        ProductType = ValidateProductType(productType);
        GameSystemId = gameSystemId;
        UpdatedAt = DateTime.UtcNow;
    }
    public void AddStock(int quantity)
    {
        if (quantity <= 0 || StockQuantity > int.MaxValue - quantity) throw new DomainException("Stock increase must be positive and within the supported range.");
        StockQuantity += quantity; UpdatedAt = DateTime.UtcNow;
    }
    public void RemoveStock(int quantity)
    {
        if (quantity <= 0) throw new DomainException("Stock decrease must be positive.");
        if (quantity > StockQuantity) throw new DomainException("There is not enough stock.");
        StockQuantity -= quantity; UpdatedAt = DateTime.UtcNow;
    }
    public void Deactivate() { IsActive = false; UpdatedAt = DateTime.UtcNow; }
    private static void ValidatePrice(decimal price) { if (price <= 0) throw new DomainException("Product price must be greater than zero."); }
    private static string ValidateProductType(string productType)
    {
        var value = Required(productType, nameof(productType), 20);
        if (value is not ("CoreBook" or "Adventure" or "Dice" or "Accessory" or "Digital"))
            throw new DomainException("Product type must be CoreBook, Adventure, Dice, Accessory, or Digital.");
        return value;
    }
    private static string Required(string value, string field, int max)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new DomainException($"{field} is required.");
        var text = value.Trim();
        if (text.Length > max) throw new DomainException($"{field} cannot exceed {max} characters.");
        return text;
    }
}

/// <summary>ProductId is both the PK and FK, implementing the dependent side of 1:1.</summary>
public sealed class ProductDetail
{
    private ProductDetail() { }
    public ProductDetail(Guid productId, string description, string? manufacturer)
    {
        if (productId == Guid.Empty) throw new DomainException("Product id is required for product details.");
        if (string.IsNullOrWhiteSpace(description) || description.Trim().Length > 2000) throw new DomainException("Detailed description is required and must not exceed 2000 characters.");
        if (manufacturer?.Trim().Length > 120) throw new DomainException("Manufacturer cannot exceed 120 characters.");
        ProductId = productId; Description = description.Trim();
        Manufacturer = string.IsNullOrWhiteSpace(manufacturer) ? null : manufacturer.Trim();
    }
    public Guid ProductId { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public string? Manufacturer { get; private set; }
    public Product Product { get; private set; } = null!;
}

public sealed class ProductImage
{
    private ProductImage() { }
    public ProductImage(Guid productId, string url, int sortOrder)
    {
        if (productId == Guid.Empty) throw new DomainException("Product id is required for an image.");
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https")) throw new DomainException("Image URL must be an absolute HTTP(S) URL.");
        if (sortOrder < 0) throw new DomainException("Image sort order cannot be negative.");
        ProductId = productId; Url = uri.AbsoluteUri; SortOrder = sortOrder;
    }
    public int ProductImageId { get; private set; }
    public Guid ProductId { get; private set; }
    public string Url { get; private set; } = string.Empty;
    public int SortOrder { get; private set; }
    public Product Product { get; private set; } = null!;
}

public sealed class Category
{
    private Category() { }
    public Category(int id, string name)
    {
        if (id <= 0 || string.IsNullOrWhiteSpace(name) || name.Trim().Length > 80) throw new DomainException("Category id must be positive and name must be 1–80 characters.");
        CategoryId = id; Name = name.Trim();
    }
    public int CategoryId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public ICollection<ProductCategory> ProductCategories { get; private set; } = new List<ProductCategory>();
}

/// <summary>Explicit many-to-many join entity with its own association metadata.</summary>
public sealed class ProductCategory
{
    private ProductCategory() { }
    public ProductCategory(Guid productId, int categoryId, DateTime? addedAt = null, string assignedBy = "catalog-api")
    {
        if (productId == Guid.Empty || categoryId <= 0) throw new DomainException("Product and category ids are required.");
        if (string.IsNullOrWhiteSpace(assignedBy) || assignedBy.Trim().Length > 100) throw new DomainException("AssignedBy is required and cannot exceed 100 characters.");
        ProductId = productId; CategoryId = categoryId; AddedAt = addedAt ?? DateTime.UtcNow; AssignedBy = assignedBy.Trim();
    }
    public Guid ProductId { get; private set; }
    public int CategoryId { get; private set; }
    public DateTime AddedAt { get; private set; }
    public string AssignedBy { get; private set; } = string.Empty;
    public Product Product { get; private set; } = null!;
    public Category Category { get; private set; } = null!;
}

public sealed class CatalogCollection
{
    private CatalogCollection() { }
    public CatalogCollection(Guid id, string name, Guid? gameSystemId, string description = "", bool isPublished = false)
    {
        if (id == Guid.Empty || string.IsNullOrWhiteSpace(name) || name.Trim().Length > 120) throw new DomainException("Collection id and a 1–120 character name are required.");
        if (description?.Trim().Length > 500) throw new DomainException("Collection description cannot exceed 500 characters.");
        CollectionId = id; Name = name.Trim(); GameSystemId = gameSystemId; Description = description?.Trim() ?? string.Empty; IsPublished = isPublished;
    }
    public Guid CollectionId { get; private set; }
    public Guid? GameSystemId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public bool IsPublished { get; private set; }
    public GameSystem? GameSystem { get; private set; }
    public ICollection<CollectionItem> Items { get; private set; } = new List<CollectionItem>();
}

public sealed class CollectionItem
{
    private CollectionItem() { }
    public CollectionItem(Guid collectionId, Guid productId, short position, DateTime? addedAt = null)
    {
        if (collectionId == Guid.Empty || productId == Guid.Empty || position <= 0) throw new DomainException("Collection/product ids and positive position are required.");
        CollectionId = collectionId; ProductId = productId; Position = position; AddedAt = addedAt ?? DateTime.UtcNow;
    }
    public Guid CollectionId { get; private set; }
    public Guid ProductId { get; private set; }
    public short Position { get; private set; }
    public DateTime AddedAt { get; private set; }
    public CatalogCollection Collection { get; private set; } = null!;
    public Product Product { get; private set; } = null!;
}
