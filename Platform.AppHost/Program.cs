var builder=DistributedApplication.CreateBuilder(args);
var sql=builder.AddSqlServer("sql").WithDataVolume();
var ordersDb=sql.AddDatabase("RpgOrdersDb");
builder.AddProject<Projects.Orders_Api>("orders-api").WithReference(ordersDb).WaitFor(ordersDb);
builder.Build().Run();
