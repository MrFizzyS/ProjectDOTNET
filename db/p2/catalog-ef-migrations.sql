IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026112149_InitialCreate'
)
BEGIN
    CREATE TABLE [Category] (
        [CategoryId] int NOT NULL IDENTITY,
        [Name] nvarchar(80) NOT NULL,
        CONSTRAINT [PK_Category] PRIMARY KEY ([CategoryId])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026112149_InitialCreate'
)
BEGIN
    CREATE TABLE [GameSystem] (
        [GameSystemId] uniqueidentifier NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [Publisher] nvarchar(120) NOT NULL,
        [Edition] nvarchar(40) NOT NULL,
        CONSTRAINT [PK_GameSystem] PRIMARY KEY ([GameSystemId])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026112149_InitialCreate'
)
BEGIN
    CREATE TABLE [Collection] (
        [CollectionId] uniqueidentifier NOT NULL,
        [GameSystemId] uniqueidentifier NULL,
        [Name] nvarchar(120) NOT NULL,
        [Description] nvarchar(500) NOT NULL DEFAULT N'',
        [IsPublished] bit NOT NULL DEFAULT CAST(0 AS bit),
        CONSTRAINT [PK_Collection] PRIMARY KEY ([CollectionId]),
        CONSTRAINT [FK_Collection_GameSystem_GameSystemId] FOREIGN KEY ([GameSystemId]) REFERENCES [GameSystem] ([GameSystemId])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026112149_InitialCreate'
)
BEGIN
    CREATE TABLE [Product] (
        [ProductId] uniqueidentifier NOT NULL,
        [GameSystemId] uniqueidentifier NULL,
        [Sku] nvarchar(40) NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [Description] nvarchar(1000) NOT NULL DEFAULT N'',
        [ProductType] varchar(20) NOT NULL,
        [UnitPrice] decimal(18,2) NOT NULL,
        [StockQuantity] int NOT NULL DEFAULT 0,
        [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
        [CreatedAt] datetime2(3) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [UpdatedAt] datetime2(3) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [RowVer] rowversion NOT NULL,
        CONSTRAINT [PK_Product] PRIMARY KEY ([ProductId]),
        CONSTRAINT [CK_Product_Price] CHECK ([UnitPrice] > 0),
        CONSTRAINT [CK_Product_Stock] CHECK ([StockQuantity] >= 0),
        CONSTRAINT [CK_Product_Type] CHECK ([ProductType] IN ('CoreBook','Adventure','Dice','Accessory','Digital')),
        CONSTRAINT [FK_Product_GameSystem_GameSystemId] FOREIGN KEY ([GameSystemId]) REFERENCES [GameSystem] ([GameSystemId])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026112149_InitialCreate'
)
BEGIN
    CREATE TABLE [CollectionItem] (
        [CollectionId] uniqueidentifier NOT NULL,
        [ProductId] uniqueidentifier NOT NULL,
        [Position] smallint NOT NULL,
        [AddedAt] datetime2(3) NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_CollectionItem] PRIMARY KEY ([CollectionId], [ProductId]),
        CONSTRAINT [CK_CollectionItem_Position] CHECK ([Position] > 0),
        CONSTRAINT [FK_CollectionItem_Collection_CollectionId] FOREIGN KEY ([CollectionId]) REFERENCES [Collection] ([CollectionId]) ON DELETE CASCADE,
        CONSTRAINT [FK_CollectionItem_Product_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Product] ([ProductId]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026112149_InitialCreate'
)
BEGIN
    CREATE TABLE [ProductCategory] (
        [ProductId] uniqueidentifier NOT NULL,
        [CategoryId] int NOT NULL,
        [AssignedAt] datetime2(3) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [AssignedBy] nvarchar(100) NOT NULL DEFAULT (SUSER_SNAME()),
        CONSTRAINT [PK_ProductCategory] PRIMARY KEY ([ProductId], [CategoryId]),
        CONSTRAINT [FK_ProductCategory_Category_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [Category] ([CategoryId]) ON DELETE CASCADE,
        CONSTRAINT [FK_ProductCategory_Product_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Product] ([ProductId]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026112149_InitialCreate'
)
BEGIN
    CREATE TABLE [ProductDetail] (
        [ProductId] uniqueidentifier NOT NULL,
        [Description] nvarchar(2000) NOT NULL,
        [Manufacturer] nvarchar(120) NULL,
        CONSTRAINT [PK_ProductDetail] PRIMARY KEY ([ProductId]),
        CONSTRAINT [FK_ProductDetail_Product_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Product] ([ProductId]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026112149_InitialCreate'
)
BEGIN
    CREATE TABLE [ProductImage] (
        [ProductImageId] int NOT NULL IDENTITY,
        [ProductId] uniqueidentifier NOT NULL,
        [Url] nvarchar(500) NOT NULL,
        [SortOrder] int NOT NULL,
        CONSTRAINT [PK_ProductImage] PRIMARY KEY ([ProductImageId]),
        CONSTRAINT [CK_ProductImage_SortOrder] CHECK ([SortOrder] >= 0),
        CONSTRAINT [FK_ProductImage_Product_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Product] ([ProductId]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026112149_InitialCreate'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'CategoryId', N'Name') AND [object_id] = OBJECT_ID(N'[Category]'))
        SET IDENTITY_INSERT [Category] ON;
    EXEC(N'INSERT INTO [Category] ([CategoryId], [Name])
    VALUES (1, N''Core Rulebooks''),
    (2, N''Adventures''),
    (3, N''Dice''),
    (4, N''Accessories'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'CategoryId', N'Name') AND [object_id] = OBJECT_ID(N'[Category]'))
        SET IDENTITY_INSERT [Category] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026112149_InitialCreate'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'GameSystemId', N'Edition', N'Name', N'Publisher') AND [object_id] = OBJECT_ID(N'[GameSystem]'))
        SET IDENTITY_INSERT [GameSystem] ON;
    EXEC(N'INSERT INTO [GameSystem] ([GameSystemId], [Edition], [Name], [Publisher])
    VALUES (''aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa'', N''5e'', N''Dungeons & Dragons'', N''Wizards of the Coast'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'GameSystemId', N'Edition', N'Name', N'Publisher') AND [object_id] = OBJECT_ID(N'[GameSystem]'))
        SET IDENTITY_INSERT [GameSystem] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026112149_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Category_Name] ON [Category] ([Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026112149_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Collection_GameSystemId] ON [Collection] ([GameSystemId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026112149_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Collection_Name] ON [Collection] ([Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026112149_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_CollectionItem_CollectionId_Position] ON [CollectionItem] ([CollectionId], [Position]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026112149_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_CollectionItem_Product] ON [CollectionItem] ([ProductId], [CollectionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026112149_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_GameSystem_Name_Edition] ON [GameSystem] ([Name], [Edition]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026112149_InitialCreate'
)
BEGIN
    EXEC(N'CREATE INDEX [IX_Product_Active_Type_Price] ON [Product] ([ProductType], [UnitPrice]) INCLUDE ([Name], [Sku], [StockQuantity]) WHERE [IsActive] = 1');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026112149_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Product_GameSystemId] ON [Product] ([GameSystemId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026112149_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Product_Name] ON [Product] ([Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026112149_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Product_Sku] ON [Product] ([Sku]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026112149_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ProductCategory_Category] ON [ProductCategory] ([CategoryId], [ProductId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026112149_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ProductImage_ProductId_SortOrder] ON [ProductImage] ([ProductId], [SortOrder]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026112149_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261026112149_InitialCreate', N'9.0.0');
END;

COMMIT;
GO

