using Catalog.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Dal.Configurations;

public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Product", table =>
        {
            table.HasCheckConstraint("CK_Product_Type", "[ProductType] IN ('CoreBook','Adventure','Dice','Accessory','Digital')");
            table.HasCheckConstraint("CK_Product_Price", "[UnitPrice] > 0");
            table.HasCheckConstraint("CK_Product_Stock", "[StockQuantity] >= 0");
        });
        builder.HasKey(x => x.ProductId);
        builder.Property(x => x.ProductId).ValueGeneratedNever();
        builder.Property(x => x.Sku).HasMaxLength(40).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(1000).HasDefaultValue("").IsRequired();
        builder.Property(x => x.ProductType).HasColumnType("varchar(20)").IsRequired();
        builder.Property(x => x.UnitPrice).HasPrecision(18, 2);
        builder.Property(x => x.StockQuantity).HasDefaultValue(0);
        builder.Property(x => x.IsActive).HasDefaultValue(true);
        builder.Property(x => x.CreatedAt).HasColumnType("datetime2(3)").HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(x => x.UpdatedAt).HasColumnType("datetime2(3)").HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(x => x.RowVersion).HasColumnName("RowVer").IsRowVersion();
        builder.HasIndex(x => x.Sku).IsUnique();
        builder.HasIndex(x => x.Name);
        builder.HasIndex(x => new { x.ProductType, x.UnitPrice })
            .HasDatabaseName("IX_Product_Active_Type_Price").HasFilter("[IsActive] = 1")
            .IncludeProperties(x => new { x.Name, x.Sku, x.StockQuantity });
        builder.HasOne(x => x.GameSystem).WithMany(x => x.Products).HasForeignKey(x => x.GameSystemId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(x => x.Detail).WithOne(x => x.Product).HasForeignKey<ProductDetail>(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(x => x.Images).WithOne(x => x.Product).HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(x => x.ProductCategories).WithOne(x => x.Product).HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(x => x.CollectionItems).WithOne(x => x.Product).HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
    }
}
