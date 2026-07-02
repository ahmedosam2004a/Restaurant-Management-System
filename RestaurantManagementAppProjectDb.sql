/*
    RestaurantManagementAppProjectDb
    SQL Server schema and seed data for the WinForms restaurant manager.

    This script creates the database files inside the project Database folder,
    then builds the schema and sample data.
*/

IF DB_ID(N'RestaurantManagementAppProjectDb') IS NULL
BEGIN
    EXEC(N'
        CREATE DATABASE [RestaurantManagementAppProjectDb]
        ON PRIMARY
        (
            NAME = N''RestaurantManagementAppProjectDb'',
            FILENAME = N''C:\Users\DELL\OneDrive\Desktop\RestaurantManagementApp\Database\RestaurantManagementAppProjectDb.mdf''
        )
        LOG ON
        (
            NAME = N''RestaurantManagementAppProjectDb_Log'',
            FILENAME = N''C:\Users\DELL\OneDrive\Desktop\RestaurantManagementApp\Database\RestaurantManagementAppProjectDb_log.ldf''
        );
    ');
END
GO

USE [RestaurantManagementAppProjectDb];
GO

IF OBJECT_ID(N'dbo.OrderItems', N'U') IS NOT NULL
    DROP TABLE dbo.OrderItems;

IF OBJECT_ID(N'dbo.[Orders]', N'U') IS NOT NULL
    DROP TABLE dbo.[Orders];

IF OBJECT_ID(N'dbo.RestaurantTables', N'U') IS NOT NULL
    DROP TABLE dbo.RestaurantTables;

IF OBJECT_ID(N'dbo.MenuItems', N'U') IS NOT NULL
    DROP TABLE dbo.MenuItems;

IF OBJECT_ID(N'dbo.Categories', N'U') IS NOT NULL
    DROP TABLE dbo.Categories;

CREATE TABLE dbo.Categories
(
    Id INT NOT NULL CONSTRAINT PK_Categories PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL CONSTRAINT UQ_Categories_Name UNIQUE
);

CREATE TABLE dbo.MenuItems
(
    Id INT NOT NULL CONSTRAINT PK_MenuItems PRIMARY KEY,
    Name NVARCHAR(200) NOT NULL,
    Category NVARCHAR(100) NOT NULL,
    Price DECIMAL(18, 2) NOT NULL
);

CREATE TABLE dbo.RestaurantTables
(
    Id INT NOT NULL CONSTRAINT PK_RestaurantTables PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    Seats INT NOT NULL,
    IsOccupied BIT NOT NULL,
    ActiveOrderId INT NULL
);

CREATE TABLE dbo.[Orders]
(
    Id INT NOT NULL CONSTRAINT PK_Orders PRIMARY KEY,
    TableId INT NOT NULL,
    TableName NVARCHAR(100) NOT NULL,
    CreatedAt DATETIME2 NOT NULL,
    ClosedAt DATETIME2 NULL
);

CREATE TABLE dbo.OrderItems
(
    OrderId INT NOT NULL,
    MenuItemId INT NOT NULL,
    MenuItemName NVARCHAR(200) NOT NULL,
    UnitPrice DECIMAL(18, 2) NOT NULL,
    Quantity INT NOT NULL,
    CONSTRAINT PK_OrderItems PRIMARY KEY (OrderId, MenuItemId)
);

INSERT INTO dbo.Categories (Id, Name) VALUES
(1, N'ساندوتشات'),
(2, N'وجبات'),
(3, N'بيتزا'),
(4, N'مقبلات'),
(5, N'مشروبات');

INSERT INTO dbo.MenuItems (Id, Name, Category, Price) VALUES
(1, N'شاورما دجاج', N'ساندوتشات', 45.00),
(2, N'برجر لحم', N'وجبات', 70.00),
(3, N'بيتزا مارجريتا', N'بيتزا', 85.00),
(4, N'بطاطس مقلية', N'مقبلات', 25.00),
(5, N'عصير برتقال', N'مشروبات', 30.00),
(6, N'شاي', N'مشروبات', 15.00);

INSERT INTO dbo.RestaurantTables (Id, Name, Seats, IsOccupied, ActiveOrderId) VALUES
(1, N'طاولة 1', 2, 0, NULL),
(2, N'طاولة 2', 4, 0, NULL),
(3, N'طاولة 3', 4, 0, NULL),
(4, N'طاولة 4', 6, 0, NULL),
(5, N'طاولة VIP', 8, 0, NULL);
