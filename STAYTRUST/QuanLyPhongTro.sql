USE master;
GO

ALTER DATABASE QuanLyPhongTro
SET SINGLE_USER
WITH ROLLBACK IMMEDIATE;
GO
DROP DATABASE QuanLyPhongTro;
CREATE DATABASE QuanLyPhongTro;
GO

USE QuanLyPhongTro;
GO
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO
CREATE TABLE Users (
    UserId INT IDENTITY PRIMARY KEY,
    FullName NVARCHAR(100) NOT NULL,
	UserName VARCHAR(100) UNIQUE NOT NULL,
    Email VARCHAR(100) UNIQUE NOT NULL,
    Phone VARCHAR(15)  UNIQUE NOT NULL,
    [Password] VARCHAR(255) NOT NULL,
    [Role] VARCHAR(20) CHECK (Role IN ('Tenant', 'Landlord', 'Admin')),
    [Status] BIT DEFAULT 1,
    CreatedAt DATETIME DEFAULT GETDATE()
);
CREATE TABLE UserProfiles (
    ProfileId INT IDENTITY PRIMARY KEY,
    UserId INT NOT NULL UNIQUE,
    DateOfBirth DATE,
    Gender NVARCHAR(10),
    IdentityNumber VARCHAR(20),
    Address NVARCHAR(255),
    AvatarUrl NVARCHAR(255),
    UpdatedAt DATETIME DEFAULT GETDATE(),

    CONSTRAINT FK_Profile_User
        FOREIGN KEY (UserId) REFERENCES Users(UserId)
);

CREATE TABLE Rooms (
    RoomId INT IDENTITY PRIMARY KEY,
    LandlordId INT NOT NULL,
    Title NVARCHAR(200),
    [Description] NVARCHAR(MAX),
    Price DECIMAL(12,2) NOT NULL,
    Area FLOAT,
    [Address] NVARCHAR(255),
    [Status] VARCHAR(20) CHECK (Status IN ('Available', 'Rented')),
    CreatedAt DATETIME DEFAULT GETDATE(),

    CONSTRAINT FK_Room_Landlord
        FOREIGN KEY (LandlordId) REFERENCES Users(UserId)
);
CREATE TABLE RoomImages (
    ImageId INT IDENTITY PRIMARY KEY,
    RoomId INT NOT NULL,
    ImageUrl NVARCHAR(255),
    Approved BIT DEFAULT 0,

    CONSTRAINT FK_Image_Room
        FOREIGN KEY (RoomId) REFERENCES Rooms(RoomId)
);
CREATE TABLE RentalContracts (
    ContractId INT IDENTITY PRIMARY KEY,
    RoomId INT NOT NULL,
    TenantId INT NOT NULL,
    StartDate DATE,
    EndDate DATE,
    Deposit DECIMAL(12,2),
    [Status] VARCHAR(20) CHECK (Status IN ('Active', 'Expired', 'Cancelled')),

    CONSTRAINT FK_Contract_Room
        FOREIGN KEY (RoomId) REFERENCES Rooms(RoomId),

    CONSTRAINT FK_Contract_Tenant
        FOREIGN KEY (TenantId) REFERENCES Users(UserId)
);

-- Ensure a tenant can only have ONE active contract at a time
CREATE UNIQUE INDEX UIX_ActiveTenantContract 
ON RentalContracts (TenantId) 
WHERE [Status] = 'Active';

-- Feedbacks Table
CREATE TABLE Feedbacks (
    FeedbackId INT PRIMARY KEY IDENTITY(1,1),
    RoomId INT NOT NULL,
    UserId INT NOT NULL,
    Rating INT NOT NULL CHECK (Rating >= 1 AND Rating <= 5),
    Comment NVARCHAR(1000),
    CreatedAt DATETIME DEFAULT GETDATE(),
    CONSTRAINT FK_Feedback_Room FOREIGN KEY (RoomId) REFERENCES Rooms(RoomId),
    CONSTRAINT FK_Feedback_User FOREIGN KEY (UserId) REFERENCES Users(UserId)
);
CREATE TABLE MeterReadings (
    ReadingId INT IDENTITY PRIMARY KEY,
    RoomId INT NOT NULL,
    [Month] CHAR(7), -- YYYY-MM
    ElectricOld INT,
    ElectricNew INT,
    WaterOld INT,
    WaterNew INT,
    CreatedAt DATETIME DEFAULT GETDATE(),

    CONSTRAINT FK_Meter_Room
        FOREIGN KEY (RoomId) REFERENCES Rooms(RoomId)
);
CREATE TABLE Invoices (
    InvoiceId INT IDENTITY PRIMARY KEY,
    ContractId INT NOT NULL,
    [Month] CHAR(7),
    RoomPrice DECIMAL(12,2),
    ElectricFee DECIMAL(12,2),
    WaterFee DECIMAL(12,2),
    TotalAmount AS (RoomPrice + ElectricFee + WaterFee),
    [Status] VARCHAR(20) CHECK (Status IN ('Unpaid', 'Paid')),
    CreatedAt DATETIME DEFAULT GETDATE(),

    CONSTRAINT FK_Invoice_Contract
        FOREIGN KEY (ContractId) REFERENCES RentalContracts(ContractId)
);
CREATE TABLE Payments (
    PaymentId INT IDENTITY PRIMARY KEY,
    InvoiceId INT NOT NULL,
    PaymentMethod NVARCHAR(50),
    PaymentDate DATETIME,
    Amount DECIMAL(12,2),
    [Status] VARCHAR(20) CHECK (Status IN ('Success', 'Failed')),

    CONSTRAINT FK_Payment_Invoice
        FOREIGN KEY (InvoiceId) REFERENCES Invoices(InvoiceId)
);
CREATE TABLE ServicePackages (
    PackageId INT IDENTITY PRIMARY KEY,
    PackageName NVARCHAR(100),
    Price DECIMAL(12,2),
    DurationDays INT
);
CREATE TABLE Reports (
    ReportId INT IDENTITY PRIMARY KEY,
    ReportType NVARCHAR(50),
    CreatedBy INT,
    CreatedAt DATETIME DEFAULT GETDATE(),

    CONSTRAINT FK_Report_User
        FOREIGN KEY (CreatedBy) REFERENCES Users(UserId)
);
   CREATE TABLE Notifications (
        NotificationId INT PRIMARY KEY IDENTITY(1,1),
        UserId INT NOT NULL,
        Title NVARCHAR(200) NOT NULL,
        Message NVARCHAR(MAX) NOT NULL,
        IsRead BIT DEFAULT 0,
        CreatedAt DATETIME DEFAULT GETDATE(),
        FOREIGN KEY (UserId) REFERENCES Users(UserId)
    );
    GO

	CREATE TABLE Messages (
    MessageId INT IDENTITY PRIMARY KEY,
    SenderId INT NOT NULL,
    ReceiverId INT NOT NULL,
    RoomId INT,
    Content NVARCHAR(MAX) NOT NULL,
    IsRead BIT DEFAULT 0,
    CreatedAt DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (SenderId) REFERENCES Users(UserId),
    FOREIGN KEY (ReceiverId) REFERENCES Users(UserId),
    FOREIGN KEY (RoomId) REFERENCES Rooms(RoomId)
);

CREATE TABLE FavoriteRooms (
    FavoriteId INT IDENTITY PRIMARY KEY,
    UserId INT NOT NULL,
    RoomId INT NOT NULL,
    CreatedAt DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (UserId) REFERENCES Users(UserId),
    FOREIGN KEY (RoomId) REFERENCES Rooms(RoomId),
    UNIQUE(UserId, RoomId)
);


IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Reports') AND name = 'Description')
BEGIN
    ALTER TABLE Reports ADD Description NVARCHAR(MAX);
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Reports') AND name = 'Status')
BEGIN
    ALTER TABLE Reports ADD Status NVARCHAR(20) DEFAULT 'Pending';
END
GO
-- SEED DATA
-- Insert Users
INSERT INTO Users (FullName, UserName, Email, Phone, [Password], [Role], [Status])
VALUES 
('Duy Admin', 'duyadmin', 'admin@staytrust.io', '0912345678', '$2a$11$EUXHsMzTLDFFvPWCTeb6muptPWOavpa7PFsGwpoNaXRTJRK6ikJyi', 'Admin', 1),
('Anh Landlord', 'anhlandlord', 'landlord@staytrust.io', '0987654321', '$2a$11$EUXHsMzTLDFFvPWCTeb6muptPWOavpa7PFsGwpoNaXRTJRK6ikJyi', 'Landlord', 1),
('Tuan Tenant', 'tuantenant', 'tenant@staytrust.io', '0123456789', '$2a$11$EUXHsMzTLDFFvPWCTeb6muptPWOavpa7PFsGwpoNaXRTJRK6ikJyi', 'Tenant', 1);

-- Insert UserProfiles
INSERT INTO UserProfiles (UserId, DateOfBirth, Gender, IdentityNumber, Address)
VALUES 
(1, '1995-01-01', N'Nam', '123456789', N'Đà Nẵng'),
(2, '1990-05-15', N'Nam', '987654321', N'Hà Nội'),
(3, '2000-10-20', N'Nam', '555666777', N'Hồ Chí Minh');

-- Insert Rooms
INSERT INTO Rooms (LandlordId, Title, [Description], Price, Area, [Address], [Status])
VALUES 
(2, N'Azure Ocean Suite', N'Phòng suite cao cấp nhìn ra biển với đầy đủ tiện nghi.', 4500000, 55, N'Võ Nguyên Giáp, Sơn Trà, Đà Nẵng', 'Rented'),
(2, N'Modern Loft', N'Căn hộ loft hiện đại ngay trung tâm thành phố.', 6500000, 45, N'Sơn Trà, Đà Nẵng', 'Available'),
(2, N'Sunrise Studio', N'Phòng studio đón nắng sớm, không gian ấm cúng.', 3500000, 30, N'Ngũ Hành Sơn, Đà Nẵng', 'Available');

-- Insert RoomImages
INSERT INTO RoomImages (RoomId, ImageUrl, Approved)
VALUES 
(1, 'https://images.unsplash.com/photo-1522708323590-d24dbb6b0267?q=80&w=2070&auto=format&fit=crop', 1),
(2, 'https://images.unsplash.com/photo-1502672260266-1c1ef2d93688?q=80&w=1980&auto=format&fit=crop', 1),
(3, 'https://images.unsplash.com/photo-1493809842364-78817add7ffb?q=80&w=2070&auto=format&fit=crop', 1);

-- Insert RentalContracts
INSERT INTO RentalContracts (RoomId, TenantId, StartDate, EndDate, Deposit, [Status])
VALUES 
(1, 3, '2024-01-01', '2025-01-01', 4500000, 'Active');

-- Insert MeterReadings
INSERT INTO MeterReadings (RoomId, [Month], ElectricOld, ElectricNew, WaterOld, WaterNew)
VALUES 
(1, '2024-02', 1000, 1120, 300, 310),
(1, '2024-03', 1120, 1250, 310, 325);

-- Insert Invoices
INSERT INTO Invoices (ContractId, [Month], RoomPrice, ElectricFee, WaterFee, [Status])
VALUES 
(1, '2024-02', 4500000, 360000, 100000, 'Paid'),
(1, '2024-03', 4500000, 390000, 150000, 'Unpaid');

-- Insert Feedbacks (For previously completed stays or active ones)
INSERT INTO Feedbacks (RoomId, UserId, Rating, Comment)
VALUES 
(1, 3, 5, N'Phòng rất đẹp và sạch sẽ, chủ nhà thân thiện!');

-- Insert ServicePackages
INSERT INTO ServicePackages (PackageName, Price, DurationDays)
VALUES 
(N'Gói Cơ Bản', 100000, 30),
(N'Gói Nâng Cao', 250000, 90),
(N'Gói VIP', 500000, 180);

-- Final Select to verify
SELECT * FROM Users;
SELECT * FROM Rooms;
SELECT * FROM RentalContracts;