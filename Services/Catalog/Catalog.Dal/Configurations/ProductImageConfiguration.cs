using Catalog.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Dal.Configurations;

public sealed class ProductImageConfiguration : IEntityTypeConfiguration<ProductImage>
{
    public void Configure(EntityTypeBuilder<ProductImage> builder)
    {
        builder.ToTable("ProductImage", table => table.HasCheckConstraint("CK_ProductImage_SortOrder", "[SortOrder] >= 0"));
        builder.HasKey(x => x.ProductImageId);
        builder.Property(x => x.ProductImageId).ValueGeneratedOnAdd();
        builder.Property(x => x.Url).HasMaxLength(500).IsRequired();
        builder.HasIndex(x => new { x.ProductId, x.SortOrder }).IsUnique();
        builder.HasOne(x => x.Product).WithMany(x => x.Images).HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
    }
}
