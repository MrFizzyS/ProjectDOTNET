var builder = DistributedApplication.CreateBuilder(args);

var sqlPassword = builder.AddParameter("sql-password", secret: true);
var sqlServer = builder.AddSqlServer("sql", sqlPassword)
    .WithDataVolume();

var ordersDatabase = sqlServer.AddDatabase("RpgOrdersDb");
var catalogDatabase = sqlServer.AddDatabase("CatalogDb", "RpgCatalogDb");

var mongoServer = builder.AddMongoDB("mongo")
    .WithDataVolume();
var reviewsDatabase = mongoServer.AddDatabase("ReviewsDb", "rpg_content");

var ordersApi = builder.AddProject<Projects.Orders_Api>("orders-api")
    .WithHttpEndpoint(port: 5101, name: "http")
    .WithReference(ordersDatabase)
    .WaitFor(ordersDatabase);

var catalogApi = builder.AddProject<Projects.Catalog_Api>("catalog-api")
    .WithHttpEndpoint(port: 5102, name: "http")
    .WithReference(catalogDatabase)
    .WaitFor(catalogDatabase);

var reviewsApi = builder.AddProject<Projects.Reviews_WebApi>("reviews-api")
    .WithHttpEndpoint(port: 5103, name: "http")
    .WithReference(reviewsDatabase)
    .WaitFor(reviewsDatabase);

var aggregator = builder.AddProject<Projects.Platform_Aggregator>("aggregator")
    .WithHttpEndpoint(port: 5104, name: "http")
    .WithReference(ordersApi)
    .WaitFor(ordersApi)
    .WithReference(catalogApi)
    .WaitFor(catalogApi)
    .WithReference(reviewsApi)
    .WaitFor(reviewsApi);

builder.AddProject<Projects.Platform_ApiGateway>("gateway")
    .WithHttpEndpoint(port: 5000, name: "http")
    .WithReference(ordersApi)
    .WaitFor(ordersApi)
    .WithReference(catalogApi)
    .WaitFor(catalogApi)
    .WithReference(reviewsApi)
    .WaitFor(reviewsApi)
    .WithReference(aggregator)
    .WaitFor(aggregator);

builder.Build().Run();