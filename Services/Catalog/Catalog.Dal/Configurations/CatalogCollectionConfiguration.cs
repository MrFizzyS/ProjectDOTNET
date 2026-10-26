using Catalog.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Dal.Configurations;

public sealed class CatalogCollectionConfiguration : IEntityTypeConfiguration<CatalogCollection>
{
    public void Configure(EntityTypeBuilder<CatalogCollection> builder)
    {
        builder.ToTable("Collection");
        builder.HasKey(x => x.CollectionId);
        builder.Property(x => x.CollectionId).ValueGeneratedNever();
        builder.Property(x => x.Name).HasMaxLength(120).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(500).HasDefaultValue("").IsRequired();
        builder.Property(x => x.IsPublished).HasDefaultValue(false);
        builder.HasIndex(x => x.Name).IsUnique();
        builder.HasOne(x => x.GameSystem).WithMany(x => x.Collections).HasForeignKey(x => x.GameSystemId).OnDelete(DeleteBehavior.NoAction);
        builder.HasMany(x => x.Items).WithOne(x => x.Collection).HasForeignKey(x => x.CollectionId).OnDelete(DeleteBehavior.Cascade);
    }
}
