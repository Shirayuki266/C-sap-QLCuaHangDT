CREATE TABLE dbo.TAI_KHOAN (
    Id bigint IDENTITY PRIMARY KEY,
    Username nvarchar(100) NOT NULL UNIQUE,
    PasswordHash nvarchar(1000) NOT NULL,
    Role varchar(20) NOT NULL CHECK (Role IN ('CUSTOMER','ADMIN')),
    Active bit NOT NULL DEFAULT 1,
    CreatedAt datetime2(7) NOT NULL DEFAULT SYSUTCDATETIME()
);
CREATE TABLE dbo.KHACH_HANG (
    Id bigint PRIMARY KEY REFERENCES dbo.TAI_KHOAN(Id),
    FullName nvarchar(120) NOT NULL,
    Phone varchar(20) NOT NULL UNIQUE,
    Email nvarchar(254) NULL,
    Address nvarchar(500) NOT NULL DEFAULT N''
);
CREATE UNIQUE INDEX UX_KHACH_HANG_Email ON dbo.KHACH_HANG(Email) WHERE Email IS NOT NULL;
CREATE TABLE dbo.PHIEN (
    Id uniqueidentifier NOT NULL PRIMARY KEY,
    AccountId bigint NOT NULL REFERENCES dbo.TAI_KHOAN(Id),
    ExpiresAt datetime2(7) NOT NULL
);
CREATE INDEX IX_PHIEN_AccountId ON dbo.PHIEN(AccountId);
CREATE INDEX IX_PHIEN_ExpiresAt ON dbo.PHIEN(ExpiresAt);
CREATE TABLE dbo.HANG (Id int IDENTITY PRIMARY KEY, Name nvarchar(100) NOT NULL UNIQUE, Active bit NOT NULL DEFAULT 1);
CREATE TABLE dbo.DANH_MUC (Id int IDENTITY PRIMARY KEY, Name nvarchar(100) NOT NULL UNIQUE, Active bit NOT NULL DEFAULT 1);
CREATE TABLE dbo.SAN_PHAM (
    Id bigint IDENTITY PRIMARY KEY,
    Code nvarchar(30) NOT NULL UNIQUE,
    Name nvarchar(200) NOT NULL,
    BrandId int NOT NULL REFERENCES dbo.HANG(Id),
    CategoryId int NOT NULL REFERENCES dbo.DANH_MUC(Id),
    Specifications nvarchar(max) NOT NULL DEFAULT N'' CHECK (DATALENGTH(Specifications) <= 40000),
    Description nvarchar(max) NOT NULL DEFAULT N'' CHECK (DATALENGTH(Description) <= 100000),
    ImageUrl nvarchar(1000) NOT NULL DEFAULT N'',
    Active bit NOT NULL DEFAULT 1
);
CREATE TABLE dbo.SAN_PHAM_ANH (
    Id bigint IDENTITY PRIMARY KEY,
    ProductId bigint NOT NULL REFERENCES dbo.SAN_PHAM(Id),
    Url nvarchar(1000) NOT NULL,
    SortOrder int NOT NULL CHECK (SortOrder >= 0)
);
-- SQL Server index keys cannot hold two arbitrary 1000-character URLs.
-- Hash only for indexing; service validation also compares the full canonical URL.
ALTER TABLE dbo.SAN_PHAM_ANH ADD UrlHash AS CONVERT(binary(32),HASHBYTES('SHA2_256',Url)) PERSISTED;
CREATE UNIQUE INDEX UX_SAN_PHAM_ANH_ProductUrl ON dbo.SAN_PHAM_ANH(ProductId,UrlHash);
CREATE TABLE dbo.BIEN_THE (
    Id bigint IDENTITY PRIMARY KEY,
    ProductId bigint NOT NULL REFERENCES dbo.SAN_PHAM(Id),
    Sku nvarchar(50) NOT NULL UNIQUE,
    Color nvarchar(50) NOT NULL,
    StorageGb int NOT NULL CHECK (StorageGb > 0),
    Cost decimal(18,2) NOT NULL CHECK (Cost > 0),
    ProfitPercent decimal(7,2) NOT NULL CHECK (ProfitPercent BETWEEN 0 AND 10000),
    Price decimal(18,2) NOT NULL CHECK (Price > 0),
    Active bit NOT NULL DEFAULT 1,
    Version rowversion NOT NULL,
    CONSTRAINT UX_BIEN_THE_ProductColorStorage UNIQUE(ProductId,Color,StorageGb)
);
CREATE TABLE dbo.TON_KHO (
    VariantId bigint PRIMARY KEY REFERENCES dbo.BIEN_THE(Id),
    OnHand int NOT NULL DEFAULT 0,
    Reserved int NOT NULL DEFAULT 0,
    Version rowversion NOT NULL,
    CONSTRAINT CK_TON_KHO_Balance CHECK (Reserved >= 0 AND OnHand >= Reserved)
);
CREATE TABLE dbo.GIO_HANG_CT (
    CustomerId bigint NOT NULL REFERENCES dbo.KHACH_HANG(Id),
    VariantId bigint NOT NULL REFERENCES dbo.BIEN_THE(Id),
    Quantity int NOT NULL CHECK (Quantity > 0),
    UpdatedAt datetime2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
    PRIMARY KEY(CustomerId,VariantId)
);
CREATE TABLE dbo.DON_HANG (
    Id bigint IDENTITY PRIMARY KEY,
    Code varchar(40) NOT NULL UNIQUE,
    CustomerId bigint NOT NULL REFERENCES dbo.KHACH_HANG(Id),
    Recipient nvarchar(120) NOT NULL,
    Phone varchar(20) NOT NULL,
    Address nvarchar(500) NOT NULL,
    Status varchar(30) NOT NULL CHECK (Status IN ('CHO_XAC_NHAN','DA_XAC_NHAN','DANG_GIAO','HOAN_TAT','DA_HUY','GIAO_THAT_BAI')),
    Total decimal(18,2) NOT NULL CHECK (Total > 0),
    CreatedAt datetime2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
    CompletedAt datetime2(7) NULL,
    RequestKey uniqueidentifier NOT NULL UNIQUE CHECK (RequestKey <> '00000000-0000-0000-0000-000000000000'),
    Version rowversion NOT NULL,
    CONSTRAINT CK_DON_HANG_Completion CHECK ((Status = 'HOAN_TAT' AND CompletedAt IS NOT NULL) OR (Status <> 'HOAN_TAT' AND CompletedAt IS NULL))
);
CREATE INDEX IX_DON_HANG_CustomerCreated ON dbo.DON_HANG(CustomerId,CreatedAt);
CREATE INDEX IX_DON_HANG_Completed ON dbo.DON_HANG(Status,CompletedAt);
CREATE TABLE dbo.DON_HANG_CT (
    Id bigint IDENTITY PRIMARY KEY,
    OrderId bigint NOT NULL REFERENCES dbo.DON_HANG(Id),
    VariantId bigint NOT NULL REFERENCES dbo.BIEN_THE(Id),
    Name nvarchar(200) NOT NULL,
    Sku nvarchar(50) NOT NULL,
    Description nvarchar(500) NOT NULL,
    Quantity int NOT NULL CHECK (Quantity > 0),
    Price decimal(18,2) NOT NULL CHECK (Price > 0),
    CONSTRAINT UX_DON_HANG_CT_Variant UNIQUE(OrderId,VariantId)
);
CREATE TABLE dbo.THANH_TOAN (
    OrderId bigint PRIMARY KEY REFERENCES dbo.DON_HANG(Id),
    Method varchar(10) NOT NULL CHECK (Method = 'COD'),
    Status varchar(20) NOT NULL CHECK (Status IN ('CHUA_THU','DA_THU')),
    Amount decimal(18,2) NOT NULL,
    CollectedAt datetime2(7) NULL,
    ActorId bigint NULL REFERENCES dbo.TAI_KHOAN(Id),
    CONSTRAINT CK_THANH_TOAN_Collection CHECK (
        (Status='CHUA_THU' AND Amount=0 AND CollectedAt IS NULL AND ActorId IS NULL) OR
        (Status='DA_THU' AND Amount>0 AND CollectedAt IS NOT NULL AND ActorId IS NOT NULL))
);
CREATE TABLE dbo.LICH_SU_DON (
    Id bigint IDENTITY PRIMARY KEY,
    OrderId bigint NOT NULL REFERENCES dbo.DON_HANG(Id),
    [From] varchar(30) NULL CHECK ([From] IS NULL OR [From] IN ('CHO_XAC_NHAN','DA_XAC_NHAN','DANG_GIAO','HOAN_TAT','DA_HUY','GIAO_THAT_BAI')),
    [To] varchar(30) NOT NULL CHECK ([To] IN ('CHO_XAC_NHAN','DA_XAC_NHAN','DANG_GIAO','HOAN_TAT','DA_HUY','GIAO_THAT_BAI')),
    ActorId bigint NOT NULL REFERENCES dbo.TAI_KHOAN(Id),
    At datetime2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
    Reason nvarchar(1000) NULL
);
CREATE INDEX IX_LICH_SU_DON_Order ON dbo.LICH_SU_DON(OrderId,Id);
CREATE TABLE dbo.NHA_CUNG_CAP (
    Id bigint IDENTITY PRIMARY KEY,
    Code nvarchar(30) NOT NULL UNIQUE,
    Name nvarchar(200) NOT NULL,
    Phone varchar(20) NOT NULL,
    Address nvarchar(500) NOT NULL,
    Active bit NOT NULL DEFAULT 1
);
CREATE TABLE dbo.PHIEU_NHAP (
    Id bigint IDENTITY PRIMARY KEY,
    Code varchar(40) NOT NULL UNIQUE,
    SupplierId bigint NOT NULL REFERENCES dbo.NHA_CUNG_CAP(Id),
    CreatorId bigint NOT NULL REFERENCES dbo.TAI_KHOAN(Id),
    ConfirmerId bigint NULL REFERENCES dbo.TAI_KHOAN(Id),
    Status varchar(30) NOT NULL CHECK (Status IN ('NHAP','DA_XAC_NHAN','DA_HUY')),
    Total decimal(18,2) NOT NULL CHECK (Total >= 0),
    CreatedAt datetime2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
    ConfirmedAt datetime2(7) NULL,
    Version rowversion NOT NULL,
    CONSTRAINT CK_PHIEU_NHAP_Confirmation CHECK (
        (Status='DA_XAC_NHAN' AND ConfirmerId IS NOT NULL AND ConfirmedAt IS NOT NULL AND Total>0) OR
        (Status<>'DA_XAC_NHAN' AND ConfirmerId IS NULL AND ConfirmedAt IS NULL))
);
CREATE TABLE dbo.PHIEU_NHAP_CT (
    Id bigint IDENTITY PRIMARY KEY,
    PurchaseId bigint NOT NULL REFERENCES dbo.PHIEU_NHAP(Id),
    VariantId bigint NOT NULL REFERENCES dbo.BIEN_THE(Id),
    Quantity int NOT NULL CHECK (Quantity > 0),
    Cost decimal(18,2) NOT NULL CHECK (Cost > 0),
    CONSTRAINT UX_PHIEU_NHAP_CT_Variant UNIQUE(PurchaseId,VariantId)
);
CREATE TABLE dbo.NHAT_KY_KHO (
    Id bigint IDENTITY PRIMARY KEY,
    VariantId bigint NOT NULL REFERENCES dbo.BIEN_THE(Id),
    ActorId bigint NOT NULL REFERENCES dbo.TAI_KHOAN(Id),
    OrderId bigint NULL REFERENCES dbo.DON_HANG(Id),
    PurchaseId bigint NULL REFERENCES dbo.PHIEU_NHAP(Id),
    Kind varchar(30) NOT NULL CHECK (Kind IN ('NHAP','GIU','HUY_GIU','XUAT_GIAO','NHAN_LAI','DIEU_CHINH')),
    DeltaOnHand int NOT NULL,
    DeltaReserved int NOT NULL,
    At datetime2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
    Reason nvarchar(1000) NULL,
    OperationKey uniqueidentifier NOT NULL CHECK (OperationKey <> '00000000-0000-0000-0000-000000000000'),
    CONSTRAINT CK_NHAT_KY_KHO_Document CHECK (OrderId IS NULL OR PurchaseId IS NULL),
    CONSTRAINT CK_NHAT_KY_KHO_Delta CHECK (DeltaOnHand <> 0 OR DeltaReserved <> 0)
);
CREATE UNIQUE INDEX UX_NHAT_KY_KHO_Operation ON dbo.NHAT_KY_KHO(OperationKey,VariantId);
CREATE UNIQUE INDEX UX_NHAT_KY_KHO_OrderKind ON dbo.NHAT_KY_KHO(OrderId,VariantId,Kind) WHERE OrderId IS NOT NULL;
CREATE UNIQUE INDEX UX_NHAT_KY_KHO_PurchaseKind ON dbo.NHAT_KY_KHO(PurchaseId,VariantId,Kind) WHERE PurchaseId IS NOT NULL;
CREATE INDEX IX_NHAT_KY_KHO_Variant ON dbo.NHAT_KY_KHO(VariantId,Id);
