IF DB_ID(N'RpgCatalogDb') IS NULL CREATE DATABASE RpgCatalogDb;
GO
USE RpgCatalogDb;
GO
IF OBJECT_ID(N'dbo.GameSystem',N'U') IS NULL CREATE TABLE dbo.GameSystem(
 GameSystemId uniqueidentifier NOT NULL CONSTRAINT PK_GameSystem PRIMARY KEY DEFAULT NEWSEQUENTIALID(), Name nvarchar(100) NOT NULL, Publisher nvarchar(120) NOT NULL, Edition nvarchar(40) NOT NULL,
 CONSTRAINT UQ_GameSystem_Name_Edition UNIQUE(Name,Edition));
GO
IF OBJECT_ID(N'dbo.Product',N'U') IS NULL CREATE TABLE dbo.Product(
 ProductId uniqueidentifier NOT NULL CONSTRAINT PK_Product PRIMARY KEY DEFAULT NEWSEQUENTIALID(), GameSystemId uniqueidentifier NULL, Sku nvarchar(40) NOT NULL, Name nvarchar(200) NOT NULL, Description nvarchar(1000) NOT NULL DEFAULT N'', ProductType varchar(20) NOT NULL, UnitPrice decimal(10,2) NOT NULL, StockQuantity int NOT NULL DEFAULT 0, IsActive bit NOT NULL DEFAULT 1, CreatedAt datetime2(3) NOT NULL DEFAULT SYSUTCDATETIME(), UpdatedAt datetime2(3) NOT NULL DEFAULT SYSUTCDATETIME(), RowVer rowversion NOT NULL,
 CONSTRAINT FK_Product_GameSystem FOREIGN KEY(GameSystemId) REFERENCES dbo.GameSystem(GameSystemId), CONSTRAINT UQ_Product_Sku UNIQUE(Sku), CONSTRAINT CK_Product_Type CHECK(ProductType IN ('CoreBook','Adventure','Dice','Accessory','Digital')), CONSTRAINT CK_Product_Price CHECK(UnitPrice>=0), CONSTRAINT CK_Product_Stock CHECK(StockQuantity>=0));
GO
IF OBJECT_ID(N'dbo.Category',N'U') IS NULL CREATE TABLE dbo.Category(CategoryId int IDENTITY(1,1) CONSTRAINT PK_Category PRIMARY KEY, Name nvarchar(80) NOT NULL CONSTRAINT UQ_Category_Name UNIQUE);
GO
IF OBJECT_ID(N'dbo.ProductCategory',N'U') IS NULL CREATE TABLE dbo.ProductCategory(
 ProductId uniqueidentifier NOT NULL, CategoryId int NOT NULL, AssignedAt datetime2(3) NOT NULL DEFAULT SYSUTCDATETIME(), AssignedBy nvarchar(100) NOT NULL DEFAULT SUSER_SNAME(),
 CONSTRAINT PK_ProductCategory PRIMARY KEY(ProductId,CategoryId), CONSTRAINT FK_ProductCategory_Product FOREIGN KEY(ProductId) REFERENCES dbo.Product(ProductId) ON DELETE CASCADE, CONSTRAINT FK_ProductCategory_Category FOREIGN KEY(CategoryId) REFERENCES dbo.Category(CategoryId) ON DELETE CASCADE);
GO
IF OBJECT_ID(N'dbo.Collection',N'U') IS NULL CREATE TABLE dbo.Collection(
 CollectionId uniqueidentifier NOT NULL CONSTRAINT PK_Collection PRIMARY KEY DEFAULT NEWSEQUENTIALID(), GameSystemId uniqueidentifier NULL, Name nvarchar(120) NOT NULL, Description nvarchar(500) NOT NULL DEFAULT N'', IsPublished bit NOT NULL DEFAULT 0,
 CONSTRAINT UQ_Collection_Name UNIQUE(Name), CONSTRAINT FK_Collection_GameSystem FOREIGN KEY(GameSystemId) REFERENCES dbo.GameSystem(GameSystemId));
GO
IF OBJECT_ID(N'dbo.CollectionItem',N'U') IS NULL CREATE TABLE dbo.CollectionItem(
 CollectionId uniqueidentifier NOT NULL, ProductId uniqueidentifier NOT NULL, Position smallint NOT NULL, AddedAt datetime2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
 CONSTRAINT PK_CollectionItem PRIMARY KEY(CollectionId,ProductId), CONSTRAINT UQ_CollectionItem_Position UNIQUE(CollectionId,Position), CONSTRAINT FK_CollectionItem_Collection FOREIGN KEY(CollectionId) REFERENCES dbo.Collection(CollectionId) ON DELETE CASCADE, CONSTRAINT FK_CollectionItem_Product FOREIGN KEY(ProductId) REFERENCES dbo.Product(ProductId) ON DELETE CASCADE, CONSTRAINT CK_CollectionItem_Position CHECK(Position>0));
GO
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.Product') AND name=N'IX_Product_Active_Type_Price') CREATE INDEX IX_Product_Active_Type_Price ON dbo.Product(ProductType,UnitPrice) INCLUDE(Name,Sku,StockQuantity) WHERE IsActive=1;
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.ProductCategory') AND name=N'IX_ProductCategory_Category') CREATE INDEX IX_ProductCategory_Category ON dbo.ProductCategory(CategoryId,ProductId);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.CollectionItem') AND name=N'IX_CollectionItem_Product') CREATE INDEX IX_CollectionItem_Product ON dbo.CollectionItem(ProductId,CollectionId);
GO
