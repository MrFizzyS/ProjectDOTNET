using Catalog.Bll.Mapping;
using Catalog.Bll.Services;
using Catalog.Dal;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Catalog.Bll;

public static class CatalogBllServiceCollectionExtensions
{
    public static IServiceCollection AddCatalogBll(this IServiceCollection services, string connectionString)
    {
        services.AddCatalogDal(connectionString);
        services.AddAutoMapper(_ => { }, typeof(CatalogMappingProfile));
        services.AddValidatorsFromAssemblyContaining<Validation.CreateProductValidator>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<ICatalogStartupInitializer, CatalogStartupInitializer>();
        return services;
    }
}
