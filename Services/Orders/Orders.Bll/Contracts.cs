using System.ComponentModel.DataAnnotations;

namespace Orders.Bll;

public sealed class CustomerDto { public Guid CustomerId { get; set; } public string Email { get; set; }=""; public string DisplayName { get; set; }=""; public DateTime CreatedAt { get; set; } public DateTime UpdatedAt { get; set; } public string RowVersion { get; set; }=""; }
public sealed class CustomerWriteDto { [Required,EmailAddress,MaxLength(254)] public string Email { get; set; }=""; [Required,MinLength(2),MaxLength(120)] public string DisplayName { get; set; }=""; public string? RowVersion { get; set; } }
public sealed class ProductDto { public Guid ProductId { get; set; } public string ProductName { get; set; }=""; public string Sku { get; set; }=""; public decimal CurrentPrice { get; set; } public string RowVersion { get; set; }=""; }
public sealed class ProductWriteDto { [Required,MaxLength(200)] public string ProductName { get; set; }=""; [Required,MaxLength(40)] public string Sku { get; set; }=""; [Range(typeof(decimal),"0","99999999")] public decimal CurrentPrice { get; set; } public string? RowVersion { get; set; } }
public sealed class OrderLineDto { public long OrderLineId { get; set; } public Guid ProductId { get; set; } public string ProductNameSnapshot { get; set; }=""; public short Quantity { get; set; } public decimal UnitPriceSnapshot { get; set; } public decimal LineTotal => Quantity*UnitPriceSnapshot; }
public sealed class OrderDto { public Guid OrderId { get; set; } public Guid CustomerId { get; set; } public string? CustomerEmail { get; set; } public string? CustomerDisplayName { get; set; } public long OrderNumber { get; set; } public string Status { get; set; }=""; public DateTime? PlacedAt { get; set; } public string RowVersion { get; set; }=""; public IReadOnlyList<OrderLineDto> Lines { get; set; }=[]; }
public sealed class CreateOrderDto { [Required] public Guid CustomerId { get; set; } [Required,MinLength(1)] public List<CreateOrderLineDto> Lines { get; set; }=[]; }
public sealed class CreateOrderLineDto { [Required] public Guid ProductId { get; set; } [Range(1,99)] public short Quantity { get; set; } }
public sealed class UpdateOrderStatusDto { [Required,RegularExpression("^(Placed|Paid|Packed|Shipped|Completed|Cancelled)$")] public string Status { get; set; }=""; [Required] public string RowVersion { get; set; }=""; }
public sealed class MappingProfile : AutoMapper.Profile
{
    public MappingProfile() { CreateMap<Orders.Domain.Customer,CustomerDto>().ForMember(d=>d.RowVersion,o=>o.MapFrom(s=>Convert.ToBase64String(s.RowVer))); CreateMap<Orders.Domain.ProductSnapshot,ProductDto>().ForMember(d=>d.RowVersion,o=>o.MapFrom(s=>Convert.ToBase64String(s.RowVer))); CreateMap<Orders.Domain.OrderLine,OrderLineDto>(); CreateMap<Orders.Domain.SalesOrder,OrderDto>().ForMember(d=>d.RowVersion,o=>o.MapFrom(s=>Convert.ToBase64String(s.RowVer))).ForMember(d=>d.CustomerEmail,o=>o.MapFrom(s=>s.Customer==null?null:s.Customer.Email)).ForMember(d=>d.CustomerDisplayName,o=>o.MapFrom(s=>s.Customer==null?null:s.Customer.DisplayName)); }
}
