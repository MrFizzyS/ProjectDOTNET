namespace Orders.Domain;

public sealed class Customer { public Guid CustomerId { get; set; } public string Email { get; set; } = ""; public string DisplayName { get; set; } = ""; public DateTime CreatedAt { get; set; } public DateTime UpdatedAt { get; set; } public bool IsDeleted { get; set; } public byte[] RowVer { get; set; } = []; }
public sealed class ProductSnapshot { public Guid ProductId { get; set; } public string ProductName { get; set; } = ""; public string Sku { get; set; } = ""; public decimal CurrentPrice { get; set; } public bool IsDeleted { get; set; } public byte[] RowVer { get; set; } = []; }
public sealed class SalesOrder { public Guid OrderId { get; set; } public Guid CustomerId { get; set; } public Customer? Customer { get; set; } public long OrderNumber { get; set; } public string Status { get; set; } = "Draft"; public DateTime? PlacedAt { get; set; } public DateTime CreatedAt { get; set; } public DateTime UpdatedAt { get; set; } public bool IsDeleted { get; set; } public byte[] RowVer { get; set; } = []; }
public sealed class OrderLine { public long OrderLineId { get; set; } public Guid OrderId { get; set; } public Guid ProductId { get; set; } public string ProductNameSnapshot { get; set; } = ""; public short Quantity { get; set; } public decimal UnitPriceSnapshot { get; set; } public bool IsDeleted { get; set; } public byte[] RowVer { get; set; } = []; }

public abstract class DomainException(string message) : Exception(message);
public sealed class EntityNotFoundException(string message) : DomainException(message);
public sealed class BusinessConflictException(string message) : DomainException(message);
public sealed class DomainValidationException(string message) : DomainException(message);
