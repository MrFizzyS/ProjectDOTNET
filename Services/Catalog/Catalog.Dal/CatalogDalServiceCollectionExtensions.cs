using Catalog.Dal.Repositories;
using Catalog.Dal.Initialization;
using Catalog.Dal.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Catalog.Dal;

public static class CatalogDalServiceCollectionExtensions
{
    public static IServiceCollection AddCatalogDal(this IServiceCollection services, string connectionString)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionString));
        services.AddScoped<ICatalogDatabaseInitializer, CatalogDatabaseInitializer>();
        services.AddScoped(typeof(IGenericRepository<>), typeof(BaseEfRepository<>));
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IProductImageRepository, ProductImageRepository>();
        services.AddScoped<IProductCategoryRepository, ProductCategoryRepository>();
        services.AddScoped<ICatalogUnitOfWork, CatalogUnitOfWork>();
        return services;
    }
}
