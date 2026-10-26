using Catalog.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Dal.Configurations;

public sealed class CollectionItemConfiguration : IEntityTypeConfiguration<CollectionItem>
{
    public void Configure(EntityTypeBuilder<CollectionItem> builder)
    {
        builder.ToTable("CollectionItem", table => table.HasCheckConstraint("CK_CollectionItem_Position", "[Position] > 0"));
        builder.HasKey(x => new { x.CollectionId, x.ProductId });
        builder.Property(x => x.Position).HasColumnType("smallint");
        builder.Property(x => x.AddedAt).HasColumnType("datetime2(3)").HasDefaultValueSql("SYSUTCDATETIME()");
        builder.HasIndex(x => new { x.CollectionId, x.Position }).IsUnique();
        builder.HasIndex(x => new { x.ProductId, x.CollectionId }).HasDatabaseName("IX_CollectionItem_Product");
        builder.HasOne(x => x.Collection).WithMany(x => x.Items).HasForeignKey(x => x.CollectionId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Product).WithMany(x => x.CollectionItems).HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
    }
}
