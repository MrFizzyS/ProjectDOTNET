using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Orders.Dal;

public static class DependencyInjection
{
    public static IServiceCollection AddOrdersDal(this IServiceCollection services,IConfiguration configuration)
    {
        var connectionString=configuration.GetConnectionString("RpgOrdersDb")??throw new InvalidOperationException("Connection string 'RpgOrdersDb' is missing.");
        services.AddScoped(_=>new SqlSession(connectionString));
        services.AddScoped<ICustomerRepository,CustomerRepository>(); services.AddScoped<ISalesOrderRepository,SalesOrderRepository>(); services.AddScoped<IProductSnapshotRepository,ProductSnapshotRepository>(); services.AddScoped<IOrderLineRepository,OrderLineRepository>();
        services.AddScoped<IUnitOfWork,UnitOfWork>(); services.AddSingleton<IDatabaseInitializer>(_=>new DatabaseInitializer(connectionString));
        return services;
    }
}
