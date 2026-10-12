IF DB_ID(N'RpgOrdersDb') IS NULL CREATE DATABASE RpgOrdersDb;
GO
USE RpgOrdersDb;
GO
IF OBJECT_ID(N'dbo.Customer',N'U') IS NULL
CREATE TABLE dbo.Customer (
 CustomerId uniqueidentifier NOT NULL CONSTRAINT PK_Customer PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
 Email nvarchar(254) NOT NULL, DisplayName nvarchar(120) NOT NULL,
 CreatedAt datetime2(3) NOT NULL CONSTRAINT DF_Customer_CreatedAt DEFAULT SYSUTCDATETIME(), CreatedBy nvarchar(100) NOT NULL CONSTRAINT DF_Customer_CreatedBy DEFAULT SUSER_SNAME(),
 UpdatedAt datetime2(3) NOT NULL CONSTRAINT DF_Customer_UpdatedAt DEFAULT SYSUTCDATETIME(), UpdatedBy nvarchar(100) NOT NULL CONSTRAINT DF_Customer_UpdatedBy DEFAULT SUSER_SNAME(), IsDeleted bit NOT NULL CONSTRAINT DF_Customer_IsDeleted DEFAULT 0,
 RowVer rowversion NOT NULL, CONSTRAINT UQ_Customer_Email UNIQUE(Email), CONSTRAINT CK_Customer_Email CHECK (Email LIKE N'%_@_%._%'));
GO
IF OBJECT_ID(N'dbo.ProductSnapshot',N'U') IS NULL
CREATE TABLE dbo.ProductSnapshot (
 ProductId uniqueidentifier NOT NULL CONSTRAINT PK_ProductSnapshot PRIMARY KEY, ProductName nvarchar(200) NOT NULL, Sku nvarchar(40) NOT NULL, CurrentPrice decimal(10,2) NOT NULL,
 CreatedAt datetime2(3) NOT NULL DEFAULT SYSUTCDATETIME(), CreatedBy nvarchar(100) NOT NULL DEFAULT SUSER_SNAME(), UpdatedAt datetime2(3) NOT NULL DEFAULT SYSUTCDATETIME(), UpdatedBy nvarchar(100) NOT NULL DEFAULT SUSER_SNAME(), IsDeleted bit NOT NULL DEFAULT 0, RowVer rowversion NOT NULL,
 CONSTRAINT UQ_ProductSnapshot_Sku UNIQUE(Sku), CONSTRAINT CK_ProductSnapshot_CurrentPrice CHECK(CurrentPrice >= 0));
GO
IF OBJECT_ID(N'dbo.SalesOrder',N'U') IS NULL
CREATE TABLE dbo.SalesOrder (
 OrderId uniqueidentifier NOT NULL CONSTRAINT PK_SalesOrder PRIMARY KEY DEFAULT NEWSEQUENTIALID(), CustomerId uniqueidentifier NOT NULL,
 OrderNumber bigint IDENTITY(10000,1) NOT NULL, Status varchar(20) NOT NULL CONSTRAINT DF_SalesOrder_Status DEFAULT 'Draft', PlacedAt datetime2(3) NULL,
 CreatedAt datetime2(3) NOT NULL DEFAULT SYSUTCDATETIME(), CreatedBy nvarchar(100) NOT NULL DEFAULT SUSER_SNAME(), UpdatedAt datetime2(3) NOT NULL DEFAULT SYSUTCDATETIME(), UpdatedBy nvarchar(100) NOT NULL DEFAULT SUSER_SNAME(), IsDeleted bit NOT NULL DEFAULT 0, RowVer rowversion NOT NULL,
 CONSTRAINT UQ_SalesOrder_OrderNumber UNIQUE(OrderNumber), CONSTRAINT FK_SalesOrder_Customer FOREIGN KEY(CustomerId) REFERENCES dbo.Customer(CustomerId),
 CONSTRAINT CK_SalesOrder_Status CHECK(Status IN ('Draft','Placed','Paid','Packed','Shipped','Completed','Cancelled')));
GO
IF OBJECT_ID(N'dbo.OrderLine',N'U') IS NULL
CREATE TABLE dbo.OrderLine (
 OrderLineId bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_OrderLine PRIMARY KEY, OrderId uniqueidentifier NOT NULL, ProductId uniqueidentifier NOT NULL,
 ProductNameSnapshot nvarchar(200) NOT NULL, Quantity smallint NOT NULL, UnitPriceSnapshot decimal(10,2) NOT NULL,
 CreatedAt datetime2(3) NOT NULL DEFAULT SYSUTCDATETIME(), CreatedBy nvarchar(100) NOT NULL DEFAULT SUSER_SNAME(), UpdatedAt datetime2(3) NOT NULL DEFAULT SYSUTCDATETIME(), UpdatedBy nvarchar(100) NOT NULL DEFAULT SUSER_SNAME(), IsDeleted bit NOT NULL DEFAULT 0, RowVer rowversion NOT NULL,
 CONSTRAINT FK_OrderLine_Order FOREIGN KEY(OrderId) REFERENCES dbo.SalesOrder(OrderId) ON DELETE CASCADE,
 CONSTRAINT FK_OrderLine_ProductSnapshot FOREIGN KEY(ProductId) REFERENCES dbo.ProductSnapshot(ProductId), CONSTRAINT UQ_OrderLine_Order_Product UNIQUE(OrderId,ProductId),
 CONSTRAINT CK_OrderLine_Quantity CHECK(Quantity BETWEEN 1 AND 99), CONSTRAINT CK_OrderLine_Price CHECK(UnitPriceSnapshot >= 0));
GO
IF OBJECT_ID(N'dbo.OrderPayment',N'U') IS NULL
CREATE TABLE dbo.OrderPayment (
 OrderId uniqueidentifier NOT NULL CONSTRAINT PK_OrderPayment PRIMARY KEY, ProviderReference nvarchar(100) NULL, Amount decimal(10,2) NOT NULL, PaidAt datetime2(3) NULL,
 CreatedAt datetime2(3) NOT NULL DEFAULT SYSUTCDATETIME(), CreatedBy nvarchar(100) NOT NULL DEFAULT SUSER_SNAME(), UpdatedAt datetime2(3) NOT NULL DEFAULT SYSUTCDATETIME(), UpdatedBy nvarchar(100) NOT NULL DEFAULT SUSER_SNAME(), IsDeleted bit NOT NULL DEFAULT 0, RowVer rowversion NOT NULL,
 CONSTRAINT FK_OrderPayment_Order FOREIGN KEY(OrderId) REFERENCES dbo.SalesOrder(OrderId) ON DELETE CASCADE, CONSTRAINT UQ_OrderPayment_ProviderReference UNIQUE(ProviderReference), CONSTRAINT CK_OrderPayment_Amount CHECK(Amount >= 0));
GO
IF OBJECT_ID(N'dbo.OrderStatusHistory',N'U') IS NULL
CREATE TABLE dbo.OrderStatusHistory (
 HistoryId bigint IDENTITY(1,1) CONSTRAINT PK_OrderStatusHistory PRIMARY KEY, OrderId uniqueidentifier NOT NULL, OldStatus varchar(20) NULL, NewStatus varchar(20) NOT NULL, ChangedAt datetime2(3) NOT NULL DEFAULT SYSUTCDATETIME(), ChangedBy nvarchar(100) NOT NULL DEFAULT SUSER_SNAME(),
 CONSTRAINT FK_OrderStatusHistory_Order FOREIGN KEY(OrderId) REFERENCES dbo.SalesOrder(OrderId) ON DELETE CASCADE);
GO
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.SalesOrder') AND name=N'IX_SalesOrder_Customer_PlacedAt') CREATE INDEX IX_SalesOrder_Customer_PlacedAt ON dbo.SalesOrder(CustomerId,PlacedAt DESC) INCLUDE(Status,OrderNumber) WHERE IsDeleted=0;
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.SalesOrder') AND name=N'IX_SalesOrder_Status_PlacedAt') CREATE INDEX IX_SalesOrder_Status_PlacedAt ON dbo.SalesOrder(Status,PlacedAt) WHERE IsDeleted=0;
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.OrderLine') AND name=N'IX_OrderLine_Product') CREATE INDEX IX_OrderLine_Product ON dbo.OrderLine(ProductId) INCLUDE(OrderId,Quantity,UnitPriceSnapshot);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.OrderStatusHistory') AND name=N'IX_OrderStatusHistory_Order_Changed') CREATE INDEX IX_OrderStatusHistory_Order_Changed ON dbo.OrderStatusHistory(OrderId,ChangedAt DESC);
GO
