using Catalog.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Dal.Configurations;

public sealed class ProductDetailConfiguration : IEntityTypeConfiguration<ProductDetail>
{
    public void Configure(EntityTypeBuilder<ProductDetail> builder)
    {
        builder.ToTable("ProductDetail");
        builder.HasKey(x => x.ProductId);
        builder.Property(x => x.Description).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.Manufacturer).HasMaxLength(120);
        builder.HasOne(x => x.Product).WithOne(x => x.Detail).HasForeignKey<ProductDetail>(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
    }
}
