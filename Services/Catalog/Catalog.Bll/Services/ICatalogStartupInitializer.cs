namespace Catalog.Bll.Services;

public interface ICatalogStartupInitializer
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
}
