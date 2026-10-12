USE RpgOrdersDb;
GO
CREATE OR ALTER PROCEDURE dbo.Customer_Upsert @CustomerId uniqueidentifier=NULL,@Email nvarchar(254),@DisplayName nvarchar(120),@Actor nvarchar(100)=NULL AS
BEGIN SET NOCOUNT ON; SET @Actor=COALESCE(@Actor,SUSER_SNAME());
 IF @CustomerId IS NULL SET @CustomerId=NEWID();
 IF EXISTS(SELECT 1 FROM dbo.Customer WHERE CustomerId=@CustomerId) UPDATE dbo.Customer SET Email=@Email,DisplayName=@DisplayName,UpdatedAt=SYSUTCDATETIME(),UpdatedBy=@Actor,IsDeleted=0 WHERE CustomerId=@CustomerId;
 ELSE INSERT dbo.Customer(CustomerId,Email,DisplayName,CreatedBy,UpdatedBy) VALUES(@CustomerId,@Email,@DisplayName,@Actor,@Actor);
 SELECT * FROM dbo.Customer WHERE CustomerId=@CustomerId;
END;
GO
CREATE OR ALTER PROCEDURE dbo.Order_Create @CustomerId uniqueidentifier,@OrderId uniqueidentifier OUTPUT AS
BEGIN SET NOCOUNT ON; SET @OrderId=NEWID(); INSERT dbo.SalesOrder(OrderId,CustomerId,CreatedBy,UpdatedBy) VALUES(@OrderId,@CustomerId,SUSER_SNAME(),SUSER_SNAME());
 INSERT dbo.OrderStatusHistory(OrderId,OldStatus,NewStatus) VALUES(@OrderId,NULL,'Draft');
END;
GO
CREATE OR ALTER PROCEDURE dbo.OrderLine_Add @OrderId uniqueidentifier,@ProductId uniqueidentifier,@Quantity smallint AS
BEGIN SET NOCOUNT ON; DECLARE @Name nvarchar(200),@Price decimal(10,2);
 SELECT @Name=ProductName,@Price=CurrentPrice FROM dbo.ProductSnapshot WHERE ProductId=@ProductId AND IsDeleted=0;
 IF @Name IS NULL THROW 51001,'Product snapshot not found or deleted.',1;
 IF @Quantity NOT BETWEEN 1 AND 99 THROW 51002,'Quantity must be between 1 and 99.',1;
 INSERT dbo.OrderLine(OrderId,ProductId,ProductNameSnapshot,Quantity,UnitPriceSnapshot) VALUES(@OrderId,@ProductId,@Name,@Quantity,@Price);
END;
GO
CREATE OR ALTER PROCEDURE dbo.Order_SetStatus @OrderId uniqueidentifier,@NewStatus varchar(20),@ExpectedRowVer binary(8)=NULL,@Actor nvarchar(100)=NULL AS
BEGIN
 SET NOCOUNT ON; SET XACT_ABORT ON; SET @Actor=COALESCE(@Actor,SUSER_SNAME());
 BEGIN TRY BEGIN TRAN;
  DECLARE @OldStatus varchar(20),@Version binary(8);
  SELECT @OldStatus=Status,@Version=RowVer FROM dbo.SalesOrder WITH(UPDLOCK,HOLDLOCK) WHERE OrderId=@OrderId AND IsDeleted=0;
  IF @OldStatus IS NULL THROW 51003,'Order not found.',1;
  IF @ExpectedRowVer IS NOT NULL AND @ExpectedRowVer<>@Version THROW 51004,'Concurrency conflict: stale rowversion.',1;
  IF NOT ((@OldStatus='Draft' AND @NewStatus IN ('Placed','Cancelled')) OR (@OldStatus='Placed' AND @NewStatus IN ('Paid','Cancelled')) OR (@OldStatus='Paid' AND @NewStatus IN ('Packed','Cancelled')) OR (@OldStatus='Packed' AND @NewStatus='Shipped') OR (@OldStatus='Shipped' AND @NewStatus='Completed')) THROW 51005,'Invalid order status transition.',1;
  UPDATE dbo.SalesOrder SET Status=@NewStatus,PlacedAt=CASE WHEN @NewStatus='Placed' THEN SYSUTCDATETIME() ELSE PlacedAt END,UpdatedAt=SYSUTCDATETIME(),UpdatedBy=@Actor WHERE OrderId=@OrderId;
  INSERT dbo.OrderStatusHistory(OrderId,OldStatus,NewStatus,ChangedBy) VALUES(@OrderId,@OldStatus,@NewStatus,@Actor);
  COMMIT;
 END TRY BEGIN CATCH IF XACT_STATE()<>0 ROLLBACK; THROW; END CATCH;
END;
GO
CREATE OR ALTER PROCEDURE dbo.Order_Get @OrderId uniqueidentifier AS
BEGIN SET NOCOUNT ON; SELECT o.*,c.Email,c.DisplayName FROM dbo.SalesOrder o JOIN dbo.Customer c ON c.CustomerId=o.CustomerId WHERE o.OrderId=@OrderId AND o.IsDeleted=0;
 SELECT * FROM dbo.OrderLine WHERE OrderId=@OrderId AND IsDeleted=0 ORDER BY OrderLineId;
 SELECT * FROM dbo.OrderStatusHistory WHERE OrderId=@OrderId ORDER BY ChangedAt;
END;
GO
