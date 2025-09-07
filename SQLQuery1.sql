-- =========================
-- CREATE DATABASE
-- =========================
CREATE DATABASE ProductRegistrationPortalDB;
GO

USE ProductRegistrationPortalDB;
GO

USE master;
GO
ALTER DATABASE Product_Registration_Portal SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
DROP DATABASE Product_Registration_Portal;

-- =========================
-- USERS TABLE
-- =========================
CREATE TABLE Users (
    UserId INT IDENTITY(1,1) PRIMARY KEY,
    FirstName NVARCHAR(100) NOT NULL,
    LastName NVARCHAR(100) NOT NULL,
    Email NVARCHAR(150) UNIQUE NOT NULL,
    Password NVARCHAR(255) NOT NULL,
    Phone NVARCHAR(20),
    Address NVARCHAR(255),
    City NVARCHAR(100),
    Country NVARCHAR(100),
    Role NVARCHAR(50) DEFAULT 'User',
    IsActive BIT DEFAULT 1
);
ALTER TABLE Users ADD ProfileImage NVARCHAR(255) NULL;
ALTER TABLE Users ADD CONSTRAINT DF_ProfileImage DEFAULT '~/Content/images/default-profile.png' FOR ProfileImage;

DELETE FROM Users;

select * from Users
-- =========================
-- CATEGORIES TABLE
-- =========================
CREATE TABLE Categories (
    CategoryID INT IDENTITY(1,1) PRIMARY KEY,
    CategoryName NVARCHAR(100) NOT NULL,
    Discription NVARCHAR(100) Not NULL,
    IsActive Bit DEFAULT 1
);

-- =========================
-- PRODUCTS TABLE
-- =========================
CREATE TABLE Products (
    ProductID INT IDENTITY(1,1) PRIMARY KEY,
    Title NVARCHAR(200) NOT NULL,
    Description NVARCHAR(MAX),
    CategoryID INT FOREIGN KEY REFERENCES Categories(CategoryID),
    Price DECIMAL(18,2) NOT NULL,
    StockQuantity INT NOT NULL,
    ImagePath NVARCHAR(500),
    HoverImagePath NVARCHAR(500),
    Brand NVARCHAR(100),
    Rating DECIMAL(3,2) CHECK (Rating >= 0 AND Rating <= 5),
    IsProductActive BIT DEFAULT 1,
    CreatedAt DATETIME DEFAULT GETDATE(),
    UpdatedAt DATETIME NULL
);

-- =========================
-- ORDERS TABLE
-- =========================
CREATE TABLE Orders (
    OrderID INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT FOREIGN KEY REFERENCES Users(UserId),
    OrderDate DATETIME DEFAULT GETDATE(),
    PaymentMethod NVARCHAR(50),
    Status NVARCHAR(50) DEFAULT 'Pending',
    ShippingAddress NVARCHAR(100),

);

-- =========================
-- ORDER ITEMS TABLE
-- =========================
CREATE TABLE OrderItems (
    OrderItemID INT IDENTITY(1,1) PRIMARY KEY,
    OrderID INT FOREIGN KEY REFERENCES Orders(OrderID),
    ProductID INT FOREIGN KEY REFERENCES Products(ProductID),
    Quantity INT NOT NULL,
    UnitPrice DECIMAL(18,2) NOT NULL,
    TotalAmount AS (Quantity * UnitPrice) PERSISTED
);
