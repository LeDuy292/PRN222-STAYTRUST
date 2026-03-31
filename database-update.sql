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

-- ========== DATABASE INITIALIZATION ==========
-- Users Table
CREATE TABLE Users (
    UserId INT IDENTITY PRIMARY KEY,
    FullName NVARCHAR(100) NOT NULL,
	UserName VARCHAR(100) UNIQUE NOT NULL,
    Email VARCHAR(100) UNIQUE NOT NULL,
    Phone VARCHAR(15) UNIQUE NOT NULL,
    [Password] VARCHAR(255) NOT NULL,
    [Role] VARCHAR(20) CHECK (Role IN ('Tenant', 'Landlord', 'Admin')),
    [Status] BIT DEFAULT 1,
    CreatedAt DATETIME DEFAULT GETDATE()
);

-- UserProfiles Table
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

-- Rooms Table (Enhanced with landlord property info)
CREATE TABLE Rooms (
    RoomId INT IDENTITY PRIMARY KEY,
    LandlordId INT NOT NULL,
    Title NVARCHAR(200) NOT NULL,
    [Description] NVARCHAR(MAX),
    Price DECIMAL(12,2) NOT NULL,
    Deposit DECIMAL(12,2),
    Area FLOAT,
    [Address] NVARCHAR(255),
    [Status] VARCHAR(20) CHECK (Status IN ('Draft', 'Pending', 'Active', 'Hidden', 'Expired', 'Available', 'Rented')) DEFAULT 'Active',
    Image360Url NVARCHAR(500) NULL,
    -- Property Details
    Bedrooms INT DEFAULT 1,
    Bathrooms INT DEFAULT 1,
    Floor INT,
    BuildingFloors INT,
    YearBuilt INT,
    [Type] NVARCHAR(50) DEFAULT 'Apartment',
    Verified BIT DEFAULT 0,
    Featured BIT DEFAULT 0,
    -- Ratings & Reviews
    Rating FLOAT DEFAULT 0,
    Reviews INT DEFAULT 0,
    Views INT DEFAULT 0,
    Inquiries INT DEFAULT 0,
    -- Timestamps
    CreatedAt DATETIME DEFAULT GETDATE(),
    UpdatedAt DATETIME DEFAULT GETDATE(),
    CONSTRAINT FK_Room_Landlord
        FOREIGN KEY (LandlordId) REFERENCES Users(UserId)
);

-- RoomImages Table
CREATE TABLE RoomImages (
    ImageId INT IDENTITY PRIMARY KEY,
    RoomId INT NOT NULL,
    ImageUrl NVARCHAR(255),
    Approved BIT DEFAULT 0,
    CreatedAt DATETIME DEFAULT GETDATE(),
    CONSTRAINT FK_Image_Room
        FOREIGN KEY (RoomId) REFERENCES Rooms(RoomId)
);

-- RentalContracts Table
CREATE TABLE RentalContracts (
    ContractId INT IDENTITY PRIMARY KEY,
    RoomId INT NOT NULL,
    TenantId INT NOT NULL,
    StartDate DATE,
    EndDate DATE,
    Deposit DECIMAL(12,2),
    [Status] VARCHAR(20) CHECK (Status IN ('Active', 'Expired', 'Cancelled')) DEFAULT 'Active',
    CreatedAt DATETIME DEFAULT GETDATE(),
    CONSTRAINT FK_Contract_Room
        FOREIGN KEY (RoomId) REFERENCES Rooms(RoomId),
    CONSTRAINT FK_Contract_Tenant
        FOREIGN KEY (TenantId) REFERENCES Users(UserId)
);

-- Index for active tenant contract (removed UNIQUE constraint to allow testing with multiple rooms)
CREATE INDEX IX_ActiveTenantContract 
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

-- UtilityRates Table (Electricity and Water Prices in VND)
CREATE TABLE UtilityRates (
    RateId INT IDENTITY PRIMARY KEY,
    RoomId INT NOT NULL,
    ElectricPrice DECIMAL(10,2) NOT NULL DEFAULT 3500,
    WaterPrice DECIMAL(10,2) NOT NULL DEFAULT 12000,
    UpdatedAt DATETIME DEFAULT GETDATE(),
    CONSTRAINT FK_Rate_Room
        FOREIGN KEY (RoomId) REFERENCES Rooms(RoomId),
    UNIQUE(RoomId)
);

-- MeterReadings Table (Enhanced - Meter readings for utilities billing)
CREATE TABLE MeterReadings (
    ReadingId INT IDENTITY PRIMARY KEY,
    RoomId INT NOT NULL,
    [Month] CHAR(7),
    ElectricOld INT,
    ElectricNew INT,
    WaterOld INT,
    WaterNew INT,
    Status VARCHAR(20) DEFAULT 'Submitted', -- Submitted, Verified, Invoiced
    CreatedAt DATETIME DEFAULT GETDATE(),
    UpdatedAt DATETIME DEFAULT GETDATE(),
    CONSTRAINT FK_Meter_Room
        FOREIGN KEY (RoomId) REFERENCES Rooms(RoomId),
    UNIQUE(RoomId, [Month])
);

-- Invoices Table
CREATE TABLE Invoices (
    InvoiceId INT IDENTITY PRIMARY KEY,
    ContractId INT NOT NULL,
    [Month] CHAR(7),
    RoomPrice DECIMAL(12,2),
    ElectricFee DECIMAL(12,2),
    WaterFee DECIMAL(12,2),
    TotalAmount AS (RoomPrice + ElectricFee + WaterFee),
    [Status] VARCHAR(20) CHECK (Status IN ('Unpaid', 'Paid')) DEFAULT 'Unpaid',
    CreatedAt DATETIME DEFAULT GETDATE(),
    CONSTRAINT FK_Invoice_Contract
        FOREIGN KEY (ContractId) REFERENCES RentalContracts(ContractId)
);

-- Payments Table
CREATE TABLE Payments (
    PaymentId INT IDENTITY PRIMARY KEY,
    InvoiceId INT NOT NULL,
    PaymentMethod NVARCHAR(50),
    PaymentDate DATETIME,
    Amount DECIMAL(12,2),
    [Status] VARCHAR(20) CHECK (Status IN ('Success', 'Failed')) DEFAULT 'Success',
    CreatedAt DATETIME DEFAULT GETDATE(),
    CONSTRAINT FK_Payment_Invoice
        FOREIGN KEY (InvoiceId) REFERENCES Invoices(InvoiceId)
);

-- ServicePackages Table
CREATE TABLE ServicePackages (
    PackageId INT IDENTITY PRIMARY KEY,
    PackageName NVARCHAR(100),
    Price DECIMAL(12,2),
    DurationDays INT
);

-- Reports Table
CREATE TABLE Reports (
    ReportId INT IDENTITY PRIMARY KEY,
    ReportType NVARCHAR(50),
    Description NVARCHAR(MAX),
    [Status] NVARCHAR(20) DEFAULT 'Pending',
    CreatedBy INT NOT NULL,
    CreatedAt DATETIME DEFAULT GETDATE(),
    CONSTRAINT FK_Report_User
        FOREIGN KEY (CreatedBy) REFERENCES Users(UserId)
);

-- Notifications Table
CREATE TABLE Notifications (
    NotificationId INT PRIMARY KEY IDENTITY(1,1),
    UserId INT NOT NULL,
    Title NVARCHAR(200) NOT NULL,
    Message NVARCHAR(MAX) NOT NULL,
    IsRead BIT DEFAULT 0,
    CreatedAt DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (UserId) REFERENCES Users(UserId)
);

-- Messages Table
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

-- FavoriteRooms Table
CREATE TABLE FavoriteRooms (
    FavoriteId INT IDENTITY PRIMARY KEY,
    UserId INT NOT NULL,
    RoomId INT NOT NULL,
    CreatedAt DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (UserId) REFERENCES Users(UserId),
    FOREIGN KEY (RoomId) REFERENCES Rooms(RoomId),
    UNIQUE(UserId, RoomId)
);

GO

-- ================== SEED DATA ==================

-- Insert Users (Admin, Landlords, Tenants)
INSERT INTO Users (FullName, UserName, Email, Phone, [Password], [Role], [Status])
VALUES 
('Duy Admin', 'duyadmin', 'admin@staytrust.io', '0912345678', 'Admin@123', 'Admin', 1),
('Anh Landlord', 'anhlandlord', 'landlord@staytrust.io', '0987654321', 'Landlord@123', 'Landlord', 1),
('Tuan Tenant', 'tuantenant', 'tenant@staytrust.io', '0123456789', 'Tenant@123', 'Tenant', 1),
('T Landlord', 'tlandlord', 'tvan2015211@gmail.com', '0987650321', 'Landlord@123', 'Landlord', 1),
('Minh Tenant', 'minhtenant', 'minh@staytrust.io', '0945123456', 'Tenant@123', 'Tenant', 1),
('Hoa Landlord', 'hoalandlord', 'hoa@staytrust.io', '0967234567', 'Landlord@123', 'Landlord', 1);

-- Insert UserProfiles
INSERT INTO UserProfiles (UserId, DateOfBirth, Gender, IdentityNumber, Address)
VALUES 
(1, '1995-01-01', N'Nam', '123456789', N'Đà Nẵng'),
(2, '1990-05-15', N'Nam', '987654321', N'Hà Nội'),
(3, '2000-10-20', N'Nam', '555666777', N'Hồ Chí Minh'),
(4, '1992-03-10', N'Nam', '111222333', N'Đà Nẵng'),
(5, '1998-07-25', N'Nữ', '444555666', N'Hồ Chí Minh'),
(6, '1988-11-12', N'Nữ', '777888999', N'Hà Nội');

-- Insert Rooms (Landlord 2 - Anh Landlord)
INSERT INTO Rooms (LandlordId, Title, [Description], Price, Deposit, Area, [Address], [Status], 
                   Image360Url, Bedrooms, Bathrooms, Floor, BuildingFloors, YearBuilt, [Type], 
                   Verified, Featured, Rating, Reviews, Views, Inquiries)
VALUES 
(2, N'Luxury Penthouse Suite', N'Premium penthouse suite with ocean views and complete amenities, full city view.', 
 25000000, 50000000, 120, N'123 Nguyen Hue, District 1, HCMC', 'Rented', 
 'https://raw.githubusercontent.com/ArcherFMY/SD-T2I-360PanoImage/main/data/a-living-room.png',
 3, 2, 15, 20, 2020, 'Apartment', 1, 1, 4.8, 24, 1234, 45),

(2, N'Modern Studio Apartment', N'Modern loft apartment in the heart of the city, open space.', 
 8500000, 17000000, 45, N'456 Le Loi, District 3, HCMC', 'Rented', 
 'https://raw.githubusercontent.com/ArcherFMY/SD-T2I-360PanoImage/main/data/a-living-room.png',
 1, 1, 8, 15, 2019, 'Studio', 1, 0, 4.6, 18, 892, 32),

(2, N'Spacious Family House', N'Spacious house with 4 bedrooms, garden, suitable for large families.', 
 35000000, 70000000, 180, N'789 Tran Hung Dao, District 5, HCMC', 'Rented', 
 'https://raw.githubusercontent.com/ArcherFMY/SD-T2I-360PanoImage/main/data/a-living-room.png',
 4, 3, 0, 3, 2015, 'House', 1, 1, 4.9, 31, 2156, 67),

(2, N'Cozy Downtown Loft', N'Cozy loft with modern furniture, prime location.', 
 15000000, 30000000, 75, N'321 Pasteur, District 1, HCMC', 'Rented', 
 'https://raw.githubusercontent.com/ArcherFMY/SD-T2I-360PanoImage/main/data/a-living-room.png',
 2, 1, 5, 10, 2018, 'Loft', 1, 0, 4.5, 15, 654, 21);

-- Insert Rooms (Landlord 4 - T Landlord)
INSERT INTO Rooms (LandlordId, Title, [Description], Price, Deposit, Area, [Address], [Status], 
                   Image360Url, Bedrooms, Bathrooms, Floor, BuildingFloors, YearBuilt, [Type], 
                   Verified, Featured, Rating, Reviews, Views, Inquiries)
VALUES 
(4, N'Beachfront Villa', N'Luxury beachfront villa with 5 bedrooms, swimming pool.', 
 45000000, 90000000, 250, N'555 Vo Nguyen Giap, Son Tra, Da Nang', 'Rented', 
 'https://raw.githubusercontent.com/ArcherFMY/SD-T2I-360PanoImage/main/data/a-living-room.png',
 5, 4, 0, 2, 2021, 'Villa', 1, 1, 5.0, 42, 3421, 89),

(4, N'Executive Apartment', N'Luxury apartment for professionals, fully equipped premium amenities.', 
 18000000, 36000000, 90, N'888 Hai Ba Trung, District 3, HCMC', 'Rented', 
 'https://raw.githubusercontent.com/ArcherFMY/SD-T2I-360PanoImage/main/data/a-living-room.png',
 2, 2, 12, 18, 2022, 'Apartment', 1, 0, 4.7, 27, 1045, 38),

(4, N'Azure Ocean Suite', N'Premium suite with ocean views and complete amenities.', 
 4500000, 9000000, 55, N'123 Vo Nguyen Giap, Son Tra, Da Nang', 'Rented', 
 'https://raw.githubusercontent.com/ArcherFMY/SD-T2I-360PanoImage/main/data/a-living-room.png',
 2, 2, 3, 8, 2020, 'Apartment', 1, 1, 4.8, 22, 1100, 40);

-- Insert Rooms (Landlord 6 - Hoa Landlord)
INSERT INTO Rooms (LandlordId, Title, [Description], Price, Deposit, Area, [Address], [Status], 
                   Image360Url, Bedrooms, Bathrooms, Floor, BuildingFloors, YearBuilt, [Type], 
                   Verified, Featured, Rating, Reviews, Views, Inquiries)
VALUES 
(6, N'Sunrise Studio', N'Studio apartment with morning sunlight, cozy space, near university.', 
 3500000, 7000000, 30, N'102 Nguyen Trai, Hai Ba Trung, Ha Noi', 'Rented', 
 'https://raw.githubusercontent.com/ArcherFMY/SD-T2I-360PanoImage/main/data/a-living-room.png',
 1, 1, 2, 6, 2017, 'Studio', 1, 0, 4.5, 12, 650, 25),

(6, N'Diamond Penthouse', N'Most luxurious penthouse with city view, 5-star interior.', 
 12000000, 24000000, 120, N'999 Ly Thai To, Hoan Kiem, Ha Noi', 'Rented', 
 'https://raw.githubusercontent.com/ArcherFMY/SD-T2I-360PanoImage/main/data/a-living-room.png',
 3, 3, 20, 20, 2021, 'Penthouse', 1, 1, 4.9, 35, 1800, 60),

(6, N'Cozy Green Apartment', N'Green and cool space, quiet, suitable for working professionals.', 
 4000000, 8000000, 40, N'456 Hang Bac, Hoan Kiem, Ha Noi', 'Rented', 
 'https://raw.githubusercontent.com/ArcherFMY/SD-T2I-360PanoImage/main/data/a-living-room.png',
 2, 1, 6, 10, 2018, 'Apartment', 1, 0, 4.6, 18, 920, 33);

GO

-- Insert RoomImages
INSERT INTO RoomImages (RoomId, ImageUrl, Approved)
VALUES 
(1, 'https://images.unsplash.com/photo-1522708323590-d24dbb6b0267?q=80&w=2070&auto=format&fit=crop', 1),
(1, 'https://images.unsplash.com/photo-1600596542815-ffad4c1539a9?q=80&w=2070&auto=format&fit=crop', 1),
(2, 'https://images.unsplash.com/photo-1502672260266-1c1ef2d93688?q=80&w=1980&auto=format&fit=crop', 1),
(2, 'https://images.unsplash.com/photo-1600566753190-17f0baa2a6c3?q=80&w=2070&auto=format&fit=crop', 1),
(3, 'https://images.unsplash.com/photo-1568605114967-8130f3a36994?q=80&w=2070&auto=format&fit=crop', 1),
(3, 'https://images.unsplash.com/photo-1560448204-e02f11c3d0e2?q=80&w=2070&auto=format&fit=crop', 1),
(4, 'https://images.unsplash.com/photo-1564013799919-ab600027ffc6?q=80&w=2070&auto=format&fit=crop', 1),
(4, 'https://images.unsplash.com/photo-1613977257363-707ba9348227?q=80&w=2070&auto=format&fit=crop', 1),
(5, 'https://images.unsplash.com/photo-1493809842364-78817add7ffb?q=80&w=2070&auto=format&fit=crop', 1),
(5, 'https://images.unsplash.com/photo-1570129477492-45c003edd2be?q=80&w=2070&auto=format&fit=crop', 1),
(6, 'https://images.unsplash.com/photo-1600585154340-be6161a56a0c?q=80&w=2070&auto=format&fit=crop', 1),
(6, 'https://images.unsplash.com/photo-1600607687920-4e2a09cf159d?q=80&w=2070&auto=format&fit=crop', 1),
(7, 'https://images.unsplash.com/photo-1522708323590-d24dbb6b0267?q=80&w=2070&auto=format&fit=crop', 1),
(8, 'https://images.unsplash.com/photo-1502672260266-1c1ef2d93688?q=80&w=1980&auto=format&fit=crop', 1),
(9, 'https://images.unsplash.com/photo-1493809842364-78817add7ffb?q=80&w=2070&auto=format&fit=crop', 1),
(10, 'https://images.unsplash.com/photo-1568605114967-8130f3a36994?q=80&w=2070&auto=format&fit=crop', 1);

GO

-- Insert RentalContracts (All landlords with active tenants - Each tenant assigned to ONE room only)
INSERT INTO RentalContracts (RoomId, TenantId, StartDate, EndDate, Deposit, [Status])
VALUES 
-- Landlord 2 (Anh Landlord) - Rooms 1-4
(1, 3, '2024-01-01', '2025-01-01', 50000000, 'Active'),   -- Luxury Penthouse + Tenant Tuan
(2, 5, '2024-02-15', '2025-02-15', 17000000, 'Active'),   -- Studio + Tenant Minh
-- Landlord 4 (T Landlord) - Rooms 5-7 (Using same tenants as they can have multiple contracts in real scenario)
(5, 3, '2024-01-15', '2025-01-15', 90000000, 'Expired'),  -- Beachfront Villa + Tenant Tuan (EXPIRED)
(6, 5, '2024-02-01', '2025-02-01', 36000000, 'Expired'),  -- Executive Apartment + Tenant Minh (EXPIRED)
(7, 3, '2024-03-10', '2025-03-10', 9000000, 'Cancelled'), -- Azure Ocean Suite + Tenant Tuan (CANCELLED)
-- Landlord 6 (Hoa Landlord) - Rooms 8-10 (Create new test tenants or use same with different statuses)
(3, 3, '2024-03-01', '2025-03-01', 70000000, 'Active'),   -- Family House + Tenant Tuan (Different from Room 1)
(4, 5, '2024-04-01', '2025-04-01', 30000000, 'Active'),   -- Downtown Loft + Tenant Minh (Different from Room 2)
(8, 5, '2024-01-20', '2025-01-20', 7000000, 'Active'),    -- Sunrise Studio + Tenant Minh (EXPIRED contract)
(9, 3, '2024-02-10', '2025-02-10', 24000000, 'Active'),   -- Diamond Penthouse + Tenant Tuan
(10, 5, '2024-03-15', '2025-03-15', 8000000, 'Active');   -- Green Apartment + Tenant Minh

GO

-- Insert UtilityRates for all rooms (Electricity: 3,500 VND/kWh, Water: 12,000 VND/m³)
INSERT INTO UtilityRates (RoomId, ElectricPrice, WaterPrice, UpdatedAt)
VALUES 
(1, 3500, 12000, GETDATE()),
(2, 3500, 12000, GETDATE()),
(3, 3500, 12000, GETDATE()),
(4, 3500, 12000, GETDATE()),
(5, 3500, 12000, GETDATE()),
(6, 3500, 12000, GETDATE()),
(7, 3500, 12000, GETDATE()),
(8, 3500, 12000, GETDATE()),
(9, 3500, 12000, GETDATE()),
(10, 3500, 12000, GETDATE());

GO

-- Insert MeterReadings (Previous months for all rooms - Oct, Nov, Dec 2024)
INSERT INTO MeterReadings (RoomId, [Month], ElectricOld, ElectricNew, WaterOld, WaterNew, [Status], CreatedAt)
VALUES 
-- Room 1 (Luxury Penthouse) - Previous readings
(1, '2024-10', 900, 1000, 280, 300, 'Invoiced', '2024-10-30'),
(1, '2024-11', 1000, 1120, 300, 310, 'Invoiced', '2024-11-30'),
(1, '2024-12', 1120, 1250, 310, 325, 'Invoiced', '2024-12-30'),

-- Room 2 (Modern Studio) - Previous readings
(2, '2024-10', 700, 800, 240, 250, 'Invoiced', '2024-10-30'),
(2, '2024-11', 800, 920, 250, 265, 'Invoiced', '2024-11-30'),
(2, '2024-12', 920, 1050, 265, 280, 'Invoiced', '2024-12-30'),

-- Room 3 (Family House) - Previous readings
(3, '2024-10', 1200, 1350, 350, 370, 'Invoiced', '2024-10-30'),
(3, '2024-11', 1350, 1500, 370, 390, 'Invoiced', '2024-11-30'),
(3, '2024-12', 1500, 1680, 390, 415, 'Invoiced', '2024-12-30'),

-- Room 4 (Downtown Loft) - Previous readings
(4, '2024-10', 800, 920, 280, 300, 'Invoiced', '2024-10-30'),
(4, '2024-11', 920, 1050, 300, 320, 'Invoiced', '2024-11-30'),
(4, '2024-12', 1050, 1200, 320, 340, 'Invoiced', '2024-12-30'),

-- Room 5 (Beachfront Villa) - Previous readings
(5, '2024-10', 1500, 1700, 400, 420, 'Invoiced', '2024-10-30'),
(5, '2024-11', 1700, 1900, 420, 445, 'Invoiced', '2024-11-30'),
(5, '2024-12', 1900, 2150, 445, 475, 'Invoiced', '2024-12-30'),

-- Room 6 (Executive Apartment) - Previous readings
(6, '2024-10', 950, 1080, 310, 330, 'Invoiced', '2024-10-30'),
(6, '2024-11', 1080, 1220, 330, 350, 'Invoiced', '2024-11-30'),
(6, '2024-12', 1220, 1380, 350, 375, 'Invoiced', '2024-12-30'),

-- Room 7 (Azure Ocean Suite) - Previous readings
(7, '2024-10', 750, 860, 290, 310, 'Invoiced', '2024-10-30'),
(7, '2024-11', 860, 980, 310, 330, 'Invoiced', '2024-11-30'),
(7, '2024-12', 980, 1120, 330, 355, 'Invoiced', '2024-12-30'),

-- Room 8 (Sunrise Studio) - Previous readings
(8, '2024-10', 600, 680, 200, 210, 'Invoiced', '2024-10-30'),
(8, '2024-11', 680, 780, 210, 225, 'Invoiced', '2024-11-30'),
(8, '2024-12', 780, 890, 225, 240, 'Invoiced', '2024-12-30'),

-- Room 9 (Diamond Penthouse) - Previous readings
(9, '2024-10', 1100, 1250, 330, 350, 'Invoiced', '2024-10-30'),
(9, '2024-11', 1250, 1400, 350, 375, 'Invoiced', '2024-11-30'),
(9, '2024-12', 1400, 1580, 375, 405, 'Invoiced', '2024-12-30'),

-- Room 10 (Green Apartment) - Previous readings
(10, '2024-10', 800, 920, 270, 290, 'Invoiced', '2024-10-30'),
(10, '2024-11', 920, 1050, 290, 310, 'Invoiced', '2024-11-30'),
(10, '2024-12', 1050, 1200, 310, 335, 'Invoiced', '2024-12-30');

GO

-- Insert Invoices for previous months (Oct, Nov, Dec 2024) - All amounts in VND
-- Only creating invoices for ACTIVE contracts (ContractIds: 1, 2, 3, 4, 8, 9, 10)
INSERT INTO Invoices (ContractId, [Month], RoomPrice, ElectricFee, WaterFee, [Status], CreatedAt)
VALUES 
-- Contract 1: Room 1 (Luxury Penthouse) - October, November, December 2024
(1, '2024-10', 25000000, 350000, 240000, 'Paid', '2024-10-30'),
(1, '2024-11', 25000000, 420000, 120000, 'Paid', '2024-11-30'),
(1, '2024-12', 25000000, 455000, 180000, 'Unpaid', '2024-12-30'),

-- Contract 2: Room 2 (Modern Studio) - October, November, December 2024
(2, '2024-10', 8500000, 350000, 120000, 'Paid', '2024-10-30'),
(2, '2024-11', 8500000, 420000, 180000, 'Paid', '2024-11-30'),
(2, '2024-12', 8500000, 455000, 180000, 'Unpaid', '2024-12-30'),

-- Contract 3: Room 3 (Family House) - October, November, December 2024
(3, '2024-10', 35000000, 525000, 240000, 'Paid', '2024-10-30'),
(3, '2024-11', 35000000, 525000, 240000, 'Paid', '2024-11-30'),
(3, '2024-12', 35000000, 630000, 300000, 'Unpaid', '2024-12-30'),

-- Contract 4: Room 4 (Downtown Loft) - October, November, December 2024
(4, '2024-10', 15000000, 420000, 240000, 'Paid', '2024-10-30'),
(4, '2024-11', 15000000, 455000, 240000, 'Paid', '2024-11-30'),
(4, '2024-12', 15000000, 525000, 240000, 'Unpaid', '2024-12-30'),

-- Contract 8: Room 8 (Sunrise Studio) - October, November, December 2024
(8, '2024-10', 3500000, 280000, 120000, 'Paid', '2024-10-30'),
(8, '2024-11', 3500000, 350000, 180000, 'Paid', '2024-11-30'),
(8, '2024-12', 3500000, 385000, 180000, 'Unpaid', '2024-12-30'),

-- Contract 9: Room 9 (Diamond Penthouse) - October, November, December 2024
(9, '2024-10', 12000000, 525000, 240000, 'Paid', '2024-10-30'),
(9, '2024-11', 12000000, 525000, 300000, 'Paid', '2024-11-30'),
(9, '2024-12', 12000000, 630000, 360000, 'Unpaid', '2024-12-30'),

-- Contract 10: Room 10 (Green Apartment) - October, November, December 2024
(10, '2024-10', 4000000, 420000, 240000, 'Paid', '2024-10-30'),
(10, '2024-11', 4000000, 455000, 240000, 'Paid', '2024-11-30'),
(10, '2024-12', 4000000, 525000, 300000, 'Unpaid', '2024-12-30');

GO

-- Insert Feedbacks
INSERT INTO Feedbacks (RoomId, UserId, Rating, Comment)
VALUES 
(1, 3, 5, N'Room is very beautiful and clean, landlord is friendly!'),
(2, 5, 5, N'Great location, complete amenities'),
(3, 5, 4, N'Spacious house but a bit far from city center');

GO

-- Insert ServicePackages (Pricing in VND)
INSERT INTO ServicePackages (PackageName, Price, DurationDays)
VALUES 
(N'Basic Plan', 100000, 30),
(N'Premium Plan', 250000, 90),
(N'VIP Plan', 500000, 180);

GO

-- Final Verification
SELECT 'Users' as TableName, COUNT(*) as RecordCount FROM Users
UNION ALL
SELECT 'Rooms', COUNT(*) FROM Rooms
UNION ALL
SELECT 'RoomImages', COUNT(*) FROM RoomImages
UNION ALL
SELECT 'RentalContracts', COUNT(*) FROM RentalContracts
UNION ALL
SELECT 'UtilityRates', COUNT(*) FROM UtilityRates
UNION ALL
SELECT 'MeterReadings', COUNT(*) FROM MeterReadings
UNION ALL
SELECT 'Invoices', COUNT(*) FROM Invoices
UNION ALL
SELECT 'Feedbacks', COUNT(*) FROM Feedbacks;

GO

-- =============== DETAILED DATA INSPECTION ===============

-- 1. View all Rented Rooms with Tenant Information
SELECT 
    r.RoomId,
    r.Title,
    r.Price,
    r.[Status],
    u.FullName AS LandlordName,
    COUNT(rc.ContractId) AS ActiveContracts,
    MAX(rc.TenantId) AS TenantId
FROM Rooms r
JOIN Users u ON r.LandlordId = u.UserId
LEFT JOIN RentalContracts rc ON r.RoomId = rc.RoomId AND rc.[Status] = 'Active'
WHERE r.[Status] IN ('Rented', 'Occupied')
GROUP BY r.RoomId, r.Title, r.Price, r.[Status], u.FullName;

GO

-- 2. View Invoice Status for Current Month (Dec 2024) - All amounts in VND
SELECT 
    i.InvoiceId,
    r.Title AS RoomName,
    u.FullName AS LandlordName,
    i.[Month],
    i.RoomPrice,
    i.ElectricFee,
    i.WaterFee,
    (i.RoomPrice + i.ElectricFee + i.WaterFee) AS TotalAmount,
    i.[Status],
    i.CreatedAt
FROM Invoices i
JOIN RentalContracts rc ON i.ContractId = rc.ContractId
JOIN Rooms r ON rc.RoomId = r.RoomId
JOIN Users u ON r.LandlordId = u.UserId
WHERE i.[Month] = '2024-12'
ORDER BY u.FullName, r.Title;

GO

-- 3. View Latest Meter Readings for Each Room
SELECT 
    mr.RoomId,
    r.Title,
    u.FullName AS LandlordName,
    mr.[Month],
    mr.ElectricOld,
    mr.ElectricNew,
    (mr.ElectricNew - mr.ElectricOld) AS ElectricUsage,
    mr.WaterOld,
    mr.WaterNew,
    (mr.WaterNew - mr.WaterOld) AS WaterUsage,
    mr.[Status]
FROM MeterReadings mr
JOIN Rooms r ON mr.RoomId = r.RoomId
JOIN Users u ON r.LandlordId = u.UserId
WHERE mr.[Month] = '2024-12'
ORDER BY u.FullName, r.Title;

GO

-- 4. View Rooms Available for Billing (Rented Status + Active Contracts)
SELECT 
    r.RoomId,
    r.Title,
    r.Price,
    r.[Status],
    u.FullName AS LandlordName,
    t.FullName AS TenantName,
    rc.StartDate,
    rc.EndDate,
    ur.ElectricPrice,
    ur.WaterPrice
FROM Rooms r
JOIN Users u ON r.LandlordId = u.UserId
JOIN RentalContracts rc ON r.RoomId = rc.RoomId AND rc.[Status] = 'Active'
JOIN Users t ON rc.TenantId = t.UserId
LEFT JOIN UtilityRates ur ON r.RoomId = ur.RoomId
WHERE r.[Status] IN ('Rented', 'Occupied')
ORDER BY u.FullName, r.Title;

GO