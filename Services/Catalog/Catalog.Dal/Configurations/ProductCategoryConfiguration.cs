using Catalog.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Dal.Configurations;

public sealed class ProductCategoryConfiguration : IEntityTypeConfiguration<ProductCategory>
{
    public void Configure(EntityTypeBuilder<ProductCategory> builder)
    {
        builder.ToTable("ProductCategory");
        builder.HasKey(x => new { x.ProductId, x.CategoryId });
        builder.Property(x => x.AddedAt).HasColumnName("AssignedAt").HasColumnType("datetime2(3)").HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(x => x.AssignedBy).HasMaxLength(100).HasDefaultValueSql("SUSER_SNAME()").IsRequired();
        builder.HasIndex(x => new { x.CategoryId, x.ProductId }).HasDatabaseName("IX_ProductCategory_Category");
        builder.HasOne(x => x.Product).WithMany(x => x.ProductCategories).HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Category).WithMany(x => x.ProductCategories).HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Cascade);
    }
}
