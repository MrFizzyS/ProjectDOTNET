USE RpgCatalogDb;
GO
DECLARE @System uniqueidentifier='aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa', @Product uniqueidentifier='22222222-2222-4222-8222-222222222222', @Collection uniqueidentifier='bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb';
IF NOT EXISTS(SELECT 1 FROM dbo.GameSystem WHERE GameSystemId=@System) INSERT dbo.GameSystem(GameSystemId,Name,Publisher,Edition) VALUES(@System,N'Dungeons & Dragons',N'Wizards of the Coast',N'5e');
IF NOT EXISTS(SELECT 1 FROM dbo.Product WHERE ProductId=@Product) INSERT dbo.Product(ProductId,GameSystemId,Sku,Name,Description,ProductType,UnitPrice,StockQuantity) VALUES(@Product,@System,N'ADV-EMBER-01',N'The Ember Crown: Adventure',N'A fantasy adventure for a party of four.',N'Adventure',24.99,12);
IF NOT EXISTS(SELECT 1 FROM dbo.Category WHERE Name=N'Adventures') INSERT dbo.Category(Name) VALUES(N'Adventures');
IF NOT EXISTS(SELECT 1 FROM dbo.ProductCategory pc JOIN dbo.Category c ON c.CategoryId=pc.CategoryId WHERE pc.ProductId=@Product AND c.Name=N'Adventures') INSERT dbo.ProductCategory(ProductId,CategoryId) SELECT @Product,CategoryId FROM dbo.Category WHERE Name=N'Adventures';
IF NOT EXISTS(SELECT 1 FROM dbo.Collection WHERE CollectionId=@Collection) INSERT dbo.Collection(CollectionId,GameSystemId,Name,Description,IsPublished) VALUES(@Collection,@System,N'New Game Master Starter Shelf',N'Essential adventures for a new GM.',1);
IF NOT EXISTS(SELECT 1 FROM dbo.CollectionItem WHERE CollectionId=@Collection AND ProductId=@Product) INSERT dbo.CollectionItem(CollectionId,ProductId,Position) VALUES(@Collection,@Product,1);
GO
