namespace Catalog.Dal.Initialization;

public interface ICatalogDatabaseInitializer
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
}
