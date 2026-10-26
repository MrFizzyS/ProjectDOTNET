using Catalog.Dal.Initialization;

namespace Catalog.Bll.Services;

/// <summary>Exposes DAL database initialization to the host without making the API depend on EF Core.</summary>
public sealed class CatalogStartupInitializer(ICatalogDatabaseInitializer databaseInitializer) : ICatalogStartupInitializer
{
    public Task InitializeAsync(CancellationToken cancellationToken = default)
        => databaseInitializer.InitializeAsync(cancellationToken);
}
