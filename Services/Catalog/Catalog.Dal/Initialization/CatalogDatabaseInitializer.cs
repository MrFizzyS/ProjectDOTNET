using Catalog.Domain;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Dal.Initialization;

public sealed class CatalogDatabaseInitializer(AppDbContext dbContext) : ICatalogDatabaseInitializer
{
    private static readonly Guid SampleProductId = Guid.Parse("22222222-2222-4222-8222-222222222222");

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await dbContext.Database.MigrateAsync(cancellationToken);

        // Reference data with fixed identifiers is inserted by HasData in IEntityTypeConfiguration.
        // This example aggregate is seeded here because it is not lookup/reference data.
        var alreadySeeded = await dbContext.Products.AsNoTracking()
            .AnyAsync(product => product.ProductId == SampleProductId || product.Sku == "ADV-EMBER-01", cancellationToken);
        if (alreadySeeded) return;

        var product = new Product(
            SampleProductId,
            "ADV-EMBER-01",
            "The Ember Crown: Adventure",
            "A fantasy adventure for a party of four.",
            "Adventure",
            24.99m,
            12,
            Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa"));
        await dbContext.Products.AddAsync(product, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
