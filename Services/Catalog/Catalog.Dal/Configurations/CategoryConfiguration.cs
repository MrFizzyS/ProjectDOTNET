using Catalog.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Dal.Configurations;

public sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Category");
        builder.HasKey(x => x.CategoryId);
        builder.Property(x => x.CategoryId).ValueGeneratedOnAdd();
        builder.Property(x => x.Name).HasMaxLength(80).IsRequired();
        builder.HasIndex(x => x.Name).IsUnique();
        builder.HasData(new Category(1, "Core Rulebooks"), new Category(2, "Adventures"), new Category(3, "Dice"), new Category(4, "Accessories"));
    }
}
