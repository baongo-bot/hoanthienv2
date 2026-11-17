-- ====================================================================
-- SCRIPT XÓA VÀ TẠO LẠI TOÀN BỘ CƠ SỞ DỮ LIỆU MYSTORE (CHUẨN KHÔNG LỖI)
-- ====================================================================

-- 1. Xóa các bảng theo thứ tự từ bảng con đến bảng cha (để tránh lỗi khóa ngoại)
IF OBJECT_ID('dbo.OrderDetail', 'U') IS NOT NULL DROP TABLE dbo.OrderDetail;
IF OBJECT_ID('dbo.Order', 'U') IS NOT NULL DROP TABLE dbo.[Order];
IF OBJECT_ID('dbo.Customer', 'U') IS NOT NULL DROP TABLE dbo.Customer;
IF OBJECT_ID('dbo.Product', 'U') IS NOT NULL DROP TABLE dbo.Product;
IF OBJECT_ID('dbo.Category', 'U') IS NOT NULL DROP TABLE dbo.Category;
IF OBJECT_ID('dbo.User', 'U') IS NOT NULL DROP TABLE dbo.[User];
GO

-- 2. Tạo lại các bảng theo thứ tự từ bảng cha đến bảng con
CREATE TABLE dbo.[User] (
    UserID INT IDENTITY(1,1) PRIMARY KEY,
    Username NVARCHAR(50) NOT NULL UNIQUE,
    Password NVARCHAR(100) NOT NULL,
    UserRole NVARCHAR(10) NOT NULL DEFAULT 'Admin'
);

CREATE TABLE dbo.Category (
    CategoryID INT IDENTITY(1,1) PRIMARY KEY,
    CategoryName NVARCHAR(100) NOT NULL
);

CREATE TABLE dbo.Product (
    ProductID INT IDENTITY(1,1) PRIMARY KEY,
    ProductName NVARCHAR(200) NOT NULL,
    ProductPrice DECIMAL(18,2) NOT NULL,
    ProductDescription NVARCHAR(MAX) NULL,
    ProductImage NVARCHAR(255) NULL,
    StockQuantity INT NOT NULL DEFAULT 0,
    CategoryID INT FOREIGN KEY REFERENCES dbo.Category(CategoryID)
);

CREATE TABLE dbo.Customer (
    CustomerID INT IDENTITY(1,1) PRIMARY KEY,
    CustomerName NVARCHAR(100) NOT NULL,
    CustomerPhone NVARCHAR(20) NOT NULL,
    CustomerAddress NVARCHAR(255) NOT NULL,
    Username NVARCHAR(50) NULL
);

CREATE TABLE dbo.[Order] (
    OrderID INT IDENTITY(1,1) PRIMARY KEY,
    OrderDate DATETIME NOT NULL DEFAULT GETDATE(),
    TotalAmount DECIMAL(18,2) NOT NULL,
    AddressDelivery NVARCHAR(255) NOT NULL,
    PaymentStatus NVARCHAR(50) NOT NULL DEFAULT N'Đang xử lý',
    CustomerID INT FOREIGN KEY REFERENCES dbo.Customer(CustomerID)
);

CREATE TABLE dbo.OrderDetail (
    OrderDetailID INT IDENTITY(1,1) PRIMARY KEY,
    Quantity INT NOT NULL,
    UnitPrice DECIMAL(18,2) NOT NULL,
    OrderID INT FOREIGN KEY REFERENCES dbo.[Order](OrderID),
    ProductID INT FOREIGN KEY REFERENCES dbo.Product(ProductID)
);
GO

-- ====================================================================
-- 3. THÊM DỮ LIỆU MẪU (SEED DATA)
-- ====================================================================
INSERT INTO dbo.[User] (Username, Password, UserRole) VALUES 
(N'admin', N'123456', N'Admin');

INSERT INTO dbo.Category (CategoryName) VALUES 
(N'Kinh Tế - Kỹ Năng'),
(N'Văn Học - Tiểu Thuyết'),
(N'Tâm Lý Học'),
(N'Lịch Sử - Xã Hội'),
(N'Sách Văn Học');

INSERT INTO dbo.Product (ProductName, ProductPrice, ProductDescription, ProductImage, StockQuantity, CategoryID) VALUES 
(N'Lý Thuyết Trò Chơi', 195000, N'Sách hay về tư duy chiến lược và kinh tế học.', N'ly-thuyet-tro-choi.jpg', 50, 1),
(N'Lịch Sử Văn Minh Thế Giới', 350000, N'Tác phẩm hoành tráng về tiến trình lịch sử nhân loại.', N'lich-su-van-minh.jpg', 30, 4),
(N'Tội Lỗi Và Hình Phạt', 220000, N'Tiểu thuyết kinh điển của văn học thế giới.', N'toi-loi-va-hinh-phat.jpg', 20, 2),
(N'Nghìn Lẻ Một Đêm', 185000, N'Tuyển tập truyện cổ tích và ngụ ngôn nổi tiếng.', N'nghin-le-mot-dem.jpg', 40, 5),
(N'Tâm Lý Học Đám Đông', 115000, N'Phân tích sâu sắc về hành vi tâm lý con người.', N'tam-ly-hoc-dam-dong.jpg', 25, 3);