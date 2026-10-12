USE RpgOrdersDb;
GO
DECLARE @Customer uniqueidentifier='11111111-1111-4111-8111-111111111111', @Product uniqueidentifier='22222222-2222-4222-8222-222222222222', @Order uniqueidentifier='33333333-3333-4333-8333-333333333333';
IF NOT EXISTS(SELECT 1 FROM dbo.Customer WHERE CustomerId=@Customer) INSERT dbo.Customer(CustomerId,Email,DisplayName) VALUES(@Customer,N'ada@example.test',N'Ada Player');
IF NOT EXISTS(SELECT 1 FROM dbo.ProductSnapshot WHERE ProductId=@Product) INSERT dbo.ProductSnapshot(ProductId,ProductName,Sku,CurrentPrice) VALUES(@Product,N'The Ember Crown: Adventure',N'ADV-EMBER-01',24.99);
IF NOT EXISTS(SELECT 1 FROM dbo.SalesOrder WHERE OrderId=@Order)
BEGIN
 INSERT dbo.SalesOrder(OrderId,CustomerId,Status,PlacedAt) VALUES(@Order,@Customer,'Draft',NULL);
 INSERT dbo.OrderStatusHistory(OrderId,OldStatus,NewStatus,ChangedBy) VALUES(@Order,NULL,'Draft',N'seed');
 INSERT dbo.OrderLine(OrderId,ProductId,ProductNameSnapshot,Quantity,UnitPriceSnapshot) VALUES(@Order,@Product,N'The Ember Crown: Adventure',1,24.99);
END;
GO
