using Catalog.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Dal.Configurations;

public sealed class GameSystemConfiguration : IEntityTypeConfiguration<GameSystem>
{
    public static readonly Guid Dnd5eId = Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa");
    public void Configure(EntityTypeBuilder<GameSystem> builder)
    {
        builder.ToTable("GameSystem");
        builder.HasKey(x => x.GameSystemId);
        builder.Property(x => x.GameSystemId).ValueGeneratedNever();
        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Publisher).HasMaxLength(120).IsRequired();
        builder.Property(x => x.Edition).HasMaxLength(40).IsRequired();
        builder.HasIndex(x => new { x.Name, x.Edition }).IsUnique();
        builder.HasData(new GameSystem(Dnd5eId, "Dungeons & Dragons", "Wizards of the Coast", "5e"));
    }
}
