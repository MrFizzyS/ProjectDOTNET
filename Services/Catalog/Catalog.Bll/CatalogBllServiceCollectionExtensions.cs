using Catalog.Bll.Mapping;
using Catalog.Bll.Services;
using Catalog.Dal;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Catalog.Bll.Caching;
using Platform.ServiceDefaults.Caching;

namespace Catalog.Bll;

public static class CatalogBllServiceCollectionExtensions
{
    public static IServiceCollection AddCatalogBll(this IServiceCollection services, string connectionString)
    {
        services.AddCatalogDal(connectionString);
        services.AddAutoMapper(_ => { }, typeof(CatalogMappingProfile));
        services.AddValidatorsFromAssemblyContaining<Validation.CreateProductValidator>();
        services.AddTieredCache();
services.AddScoped<ProductService>();
services.AddScoped<IProductService>(sp => new CachedProductService(
    sp.GetRequiredService<ProductService>(),
    sp.GetRequiredService<ITieredCache>()));
        services.AddScoped<ICatalogStartupInitializer, CatalogStartupInitializer>();
        return services;
    }
}
