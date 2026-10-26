namespace Reviews.Infrastructure.Persistence.Seeding;

public interface IDataSeeder
{
    Task SeedAsync(CancellationToken cancellationToken = default);
}
