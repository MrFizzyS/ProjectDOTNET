var builder = DistributedApplication.CreateBuilder(args);

var sqlPassword = builder.AddParameter("sql-password", secret: true);
var sqlServer = builder.AddSqlServer("sql", sqlPassword)
    .WithDataVolume();

var ordersDatabase = sqlServer.AddDatabase("RpgOrdersDb");
var catalogDatabase = sqlServer.AddDatabase("CatalogDb", "RpgCatalogDb");

var mongoServer = builder.AddMongoDB("mongo")
    .WithDataVolume();
var reviewsDatabase = mongoServer.AddDatabase("ReviewsDb", "rpg_content");

var redis = builder.AddRedis("redis")
    .WithDataVolume()
    .WithRedisInsight();

var ordersApi = builder.AddProject<Projects.Orders_Api>("orders-api")
    .WithHttpEndpoint(port: 5101, name: "http")
    .WithHttpsEndpoint(port: 7101, name: "grpc")
    .WithReference(ordersDatabase).WaitFor(ordersDatabase)
    .WithReference(redis).WaitFor(redis)
    .WithHttpHealthCheck("/health/ready", endpointName: "http");

var catalogApi = builder.AddProject<Projects.Catalog_Api>("catalog-api")
    .WithHttpEndpoint(port: 5102, name: "http")
    .WithHttpsEndpoint(port: 7102, name: "grpc")
    .WithReference(catalogDatabase).WaitFor(catalogDatabase)
    .WithReference(redis).WaitFor(redis)
    .WithHttpHealthCheck("/health/ready", endpointName: "http");

var reviewsApi = builder.AddProject<Projects.Reviews_WebApi>("reviews-api")
    .WithHttpEndpoint(port: 5103, name: "http")
    .WithHttpsEndpoint(port: 7103, name: "grpc")
    .WithReference(reviewsDatabase).WaitFor(reviewsDatabase)
    .WithReference(redis).WaitFor(redis)
    .WithHttpHealthCheck("/health/ready", endpointName: "http");

var aggregator = builder.AddProject<Projects.Platform_Aggregator>("aggregator")
    .WithHttpEndpoint(port: 5104, name: "http")
    .WithReference(ordersApi).WaitFor(ordersApi)
    .WithReference(catalogApi).WaitFor(catalogApi)
    .WithReference(reviewsApi).WaitFor(reviewsApi)
    .WithReference(redis).WaitFor(redis)
    .WithHttpHealthCheck("/health/ready", endpointName: "http");

builder.AddProject<Projects.Platform_ApiGateway>("gateway")
    .WithHttpEndpoint(port: 5000, name: "http")
    .WithReference(ordersApi).WaitFor(ordersApi)
    .WithReference(catalogApi).WaitFor(catalogApi)
    .WithReference(reviewsApi).WaitFor(reviewsApi)
    .WithReference(aggregator).WaitFor(aggregator)
    .WithHttpHealthCheck("/health/live", endpointName: "http");

builder.Build().Run();