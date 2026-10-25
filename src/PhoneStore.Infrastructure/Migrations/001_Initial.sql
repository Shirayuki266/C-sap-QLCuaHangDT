CREATE TABLE [DANH_MUC] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(100) NOT NULL,
    [Active] bit NOT NULL,
    CONSTRAINT [PK_DANH_MUC] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [HANG] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(100) NOT NULL,
    [Active] bit NOT NULL,
    CONSTRAINT [PK_HANG] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [NHA_CUNG_CAP] (
    [Id] bigint NOT NULL IDENTITY,
    [Code] nvarchar(30) NOT NULL,
    [Name] nvarchar(200) NOT NULL,
    [Phone] nvarchar(500) NULL,
    [Address] nvarchar(500) NULL,
    [Active] bit NOT NULL,
    CONSTRAINT [PK_NHA_CUNG_CAP] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [TAI_KHOAN] (
    [Id] bigint NOT NULL IDENTITY,
    [Username] nvarchar(100) NOT NULL,
    [PasswordHash] nvarchar(500) NOT NULL,
    [Role] nvarchar(500) NOT NULL,
    [Active] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_TAI_KHOAN] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_Account_Role] CHECK ([Role] IN ('ADMIN','CUSTOMER'))
);
GO


CREATE TABLE [SAN_PHAM] (
    [Id] bigint NOT NULL IDENTITY,
    [Code] nvarchar(30) NOT NULL,
    [Name] nvarchar(200) NOT NULL,
    [BrandId] int NOT NULL,
    [CategoryId] int NOT NULL,
    [Specifications] nvarchar(max) NOT NULL,
    [Description] nvarchar(max) NOT NULL,
    [ImageUrl] nvarchar(1000) NULL,
    [Active] bit NOT NULL,
    CONSTRAINT [PK_SAN_PHAM] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_SAN_PHAM_DANH_MUC_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [DANH_MUC] ([Id]),
    CONSTRAINT [FK_SAN_PHAM_HANG_BrandId] FOREIGN KEY ([BrandId]) REFERENCES [HANG] ([Id])
);
GO


CREATE TABLE [KHACH_HANG] (
    [Id] bigint NOT NULL,
    [FullName] nvarchar(120) NOT NULL,
    [Phone] nvarchar(20) NOT NULL,
    [Email] nvarchar(254) NULL,
    [Address] nvarchar(500) NULL,
    CONSTRAINT [PK_KHACH_HANG] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_KHACH_HANG_TAI_KHOAN_Id] FOREIGN KEY ([Id]) REFERENCES [TAI_KHOAN] ([Id])
);
GO


CREATE TABLE [PHIEN] (
    [Id] uniqueidentifier NOT NULL,
    [AccountId] bigint NOT NULL,
    [ExpiresAt] datetime2 NOT NULL,
    CONSTRAINT [PK_PHIEN] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_PHIEN_TAI_KHOAN_AccountId] FOREIGN KEY ([AccountId]) REFERENCES [TAI_KHOAN] ([Id])
);
GO


CREATE TABLE [PHIEU_NHAP] (
    [Id] bigint NOT NULL IDENTITY,
    [Code] nvarchar(30) NOT NULL,
    [SupplierId] bigint NOT NULL,
    [CreatorId] bigint NOT NULL,
    [ConfirmerId] bigint NULL,
    [Status] nvarchar(500) NOT NULL,
    [Total] decimal(18,2) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [ConfirmedAt] datetime2 NULL,
    [Version] rowversion NOT NULL,
    CONSTRAINT [PK_PHIEU_NHAP] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_Purchase_State] CHECK ([Total]>=0 AND [Status] IN ('NHAP','DA_XAC_NHAN','DA_HUY')),
    CONSTRAINT [FK_PHIEU_NHAP_NHA_CUNG_CAP_SupplierId] FOREIGN KEY ([SupplierId]) REFERENCES [NHA_CUNG_CAP] ([Id]),
    CONSTRAINT [FK_PHIEU_NHAP_TAI_KHOAN_ConfirmerId] FOREIGN KEY ([ConfirmerId]) REFERENCES [TAI_KHOAN] ([Id]),
    CONSTRAINT [FK_PHIEU_NHAP_TAI_KHOAN_CreatorId] FOREIGN KEY ([CreatorId]) REFERENCES [TAI_KHOAN] ([Id])
);
GO


CREATE TABLE [BIEN_THE] (
    [Id] bigint NOT NULL IDENTITY,
    [ProductId] bigint NOT NULL,
    [Sku] nvarchar(50) NOT NULL,
    [Color] nvarchar(50) NOT NULL,
    [StorageGb] int NOT NULL,
    [Price] decimal(18,2) NOT NULL,
    [Active] bit NOT NULL,
    [Version] rowversion NOT NULL,
    CONSTRAINT [PK_BIEN_THE] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_Variant_Positive] CHECK ([Price] > 0 AND [StorageGb] > 0),
    CONSTRAINT [FK_BIEN_THE_SAN_PHAM_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [SAN_PHAM] ([Id])
);
GO


CREATE TABLE [DON_HANG] (
    [Id] bigint NOT NULL IDENTITY,
    [Code] nvarchar(30) NOT NULL,
    [CustomerId] bigint NOT NULL,
    [Recipient] nvarchar(120) NOT NULL,
    [Phone] nvarchar(20) NOT NULL,
    [Address] nvarchar(500) NOT NULL,
    [Status] nvarchar(500) NOT NULL,
    [Total] decimal(18,2) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [CompletedAt] datetime2 NULL,
    [RequestKey] uniqueidentifier NOT NULL,
    [Version] rowversion NOT NULL,
    CONSTRAINT [PK_DON_HANG] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_Order_Status] CHECK ([Status] IN ('CHO_XAC_NHAN','DA_XAC_NHAN','DANG_GIAO','HOAN_TAT','DA_HUY','GIAO_THAT_BAI')),
    CONSTRAINT [CK_Order_Total] CHECK ([Total] > 0),
    CONSTRAINT [FK_DON_HANG_KHACH_HANG_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [KHACH_HANG] ([Id])
);
GO


CREATE TABLE [GIO_HANG_CT] (
    [CustomerId] bigint NOT NULL,
    [VariantId] bigint NOT NULL,
    [Quantity] int NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_GIO_HANG_CT] PRIMARY KEY ([CustomerId], [VariantId]),
    CONSTRAINT [CK_Cart_Quantity] CHECK ([Quantity] > 0),
    CONSTRAINT [FK_GIO_HANG_CT_BIEN_THE_VariantId] FOREIGN KEY ([VariantId]) REFERENCES [BIEN_THE] ([Id]),
    CONSTRAINT [FK_GIO_HANG_CT_KHACH_HANG_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [KHACH_HANG] ([Id])
);
GO


CREATE TABLE [PHIEU_NHAP_CT] (
    [Id] bigint NOT NULL IDENTITY,
    [PurchaseId] bigint NOT NULL,
    [VariantId] bigint NOT NULL,
    [Quantity] int NOT NULL,
    [Cost] decimal(18,2) NOT NULL,
    CONSTRAINT [PK_PHIEU_NHAP_CT] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_PurchaseLine_Positive] CHECK ([Quantity] > 0 AND [Cost] > 0),
    CONSTRAINT [FK_PHIEU_NHAP_CT_BIEN_THE_VariantId] FOREIGN KEY ([VariantId]) REFERENCES [BIEN_THE] ([Id]),
    CONSTRAINT [FK_PHIEU_NHAP_CT_PHIEU_NHAP_PurchaseId] FOREIGN KEY ([PurchaseId]) REFERENCES [PHIEU_NHAP] ([Id])
);
GO


CREATE TABLE [TON_KHO] (
    [VariantId] bigint NOT NULL,
    [OnHand] int NOT NULL,
    [Reserved] int NOT NULL,
    [Version] rowversion NOT NULL,
    CONSTRAINT [PK_TON_KHO] PRIMARY KEY ([VariantId]),
    CONSTRAINT [CK_Stock_Balance] CHECK ([OnHand] >= 0 AND [Reserved] >= 0 AND [Reserved] <= [OnHand]),
    CONSTRAINT [FK_TON_KHO_BIEN_THE_VariantId] FOREIGN KEY ([VariantId]) REFERENCES [BIEN_THE] ([Id])
);
GO


CREATE TABLE [DON_HANG_CT] (
    [Id] bigint NOT NULL IDENTITY,
    [OrderId] bigint NOT NULL,
    [VariantId] bigint NOT NULL,
    [Name] nvarchar(200) NOT NULL,
    [Sku] nvarchar(50) NOT NULL,
    [Description] nvarchar(100) NOT NULL,
    [Quantity] int NOT NULL,
    [Price] decimal(18,2) NOT NULL,
    CONSTRAINT [PK_DON_HANG_CT] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_OrderLine_Positive] CHECK ([Quantity] > 0 AND [Price] > 0),
    CONSTRAINT [FK_DON_HANG_CT_BIEN_THE_VariantId] FOREIGN KEY ([VariantId]) REFERENCES [BIEN_THE] ([Id]),
    CONSTRAINT [FK_DON_HANG_CT_DON_HANG_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [DON_HANG] ([Id])
);
GO


CREATE TABLE [LICH_SU_DON] (
    [Id] bigint NOT NULL IDENTITY,
    [OrderId] bigint NOT NULL,
    [From] nvarchar(500) NULL,
    [To] nvarchar(500) NOT NULL,
    [ActorId] bigint NOT NULL,
    [At] datetime2 NOT NULL,
    [Reason] nvarchar(500) NULL,
    CONSTRAINT [PK_LICH_SU_DON] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_LICH_SU_DON_DON_HANG_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [DON_HANG] ([Id]),
    CONSTRAINT [FK_LICH_SU_DON_TAI_KHOAN_ActorId] FOREIGN KEY ([ActorId]) REFERENCES [TAI_KHOAN] ([Id])
);
GO


CREATE TABLE [NHAT_KY_KHO] (
    [Id] bigint NOT NULL IDENTITY,
    [VariantId] bigint NOT NULL,
    [ActorId] bigint NOT NULL,
    [OrderId] bigint NULL,
    [PurchaseId] bigint NULL,
    [Kind] nvarchar(20) NOT NULL,
    [DeltaOnHand] int NOT NULL,
    [DeltaReserved] int NOT NULL,
    [At] datetime2 NOT NULL,
    [Reason] nvarchar(500) NULL,
    [OperationKey] uniqueidentifier NOT NULL,
    CONSTRAINT [PK_NHAT_KY_KHO] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_Movement_Delta] CHECK (([DeltaOnHand]<>0 OR [DeltaReserved]<>0) AND NOT ([OrderId] IS NOT NULL AND [PurchaseId] IS NOT NULL)),
    CONSTRAINT [CK_Movement_Kind] CHECK ([Kind] IN ('GIU','HUY_GIU','XUAT_GIAO','NHAN_LAI','NHAP','DIEU_CHINH')),
    CONSTRAINT [FK_NHAT_KY_KHO_BIEN_THE_VariantId] FOREIGN KEY ([VariantId]) REFERENCES [BIEN_THE] ([Id]),
    CONSTRAINT [FK_NHAT_KY_KHO_DON_HANG_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [DON_HANG] ([Id]),
    CONSTRAINT [FK_NHAT_KY_KHO_PHIEU_NHAP_PurchaseId] FOREIGN KEY ([PurchaseId]) REFERENCES [PHIEU_NHAP] ([Id]),
    CONSTRAINT [FK_NHAT_KY_KHO_TAI_KHOAN_ActorId] FOREIGN KEY ([ActorId]) REFERENCES [TAI_KHOAN] ([Id])
);
GO


CREATE TABLE [THANH_TOAN] (
    [OrderId] bigint NOT NULL,
    [Method] nvarchar(500) NOT NULL,
    [Status] nvarchar(500) NOT NULL,
    [Amount] decimal(18,2) NOT NULL,
    [CollectedAt] datetime2 NULL,
    [ActorId] bigint NULL,
    CONSTRAINT [PK_THANH_TOAN] PRIMARY KEY ([OrderId]),
    CONSTRAINT [CK_Payment_COD] CHECK ([Method] = 'COD' AND (([Status]='CHUA_THU' AND [Amount]=0 AND [CollectedAt] IS NULL AND [ActorId] IS NULL) OR ([Status]='DA_THU' AND [Amount]>0 AND [CollectedAt] IS NOT NULL AND [ActorId] IS NOT NULL))),
    CONSTRAINT [FK_THANH_TOAN_DON_HANG_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [DON_HANG] ([Id]),
    CONSTRAINT [FK_THANH_TOAN_TAI_KHOAN_ActorId] FOREIGN KEY ([ActorId]) REFERENCES [TAI_KHOAN] ([Id])
);
GO


CREATE UNIQUE INDEX [IX_BIEN_THE_ProductId_Color_StorageGb] ON [BIEN_THE] ([ProductId], [Color], [StorageGb]);
GO


CREATE UNIQUE INDEX [IX_BIEN_THE_Sku] ON [BIEN_THE] ([Sku]);
GO


CREATE UNIQUE INDEX [IX_DANH_MUC_Name] ON [DANH_MUC] ([Name]);
GO


CREATE UNIQUE INDEX [IX_DON_HANG_Code] ON [DON_HANG] ([Code]);
GO


CREATE INDEX [IX_DON_HANG_CustomerId_CreatedAt] ON [DON_HANG] ([CustomerId], [CreatedAt]);
GO


CREATE UNIQUE INDEX [IX_DON_HANG_RequestKey] ON [DON_HANG] ([RequestKey]);
GO


CREATE INDEX [IX_DON_HANG_Status_CreatedAt] ON [DON_HANG] ([Status], [CreatedAt]);
GO


CREATE UNIQUE INDEX [IX_DON_HANG_CT_OrderId_VariantId] ON [DON_HANG_CT] ([OrderId], [VariantId]);
GO


CREATE INDEX [IX_DON_HANG_CT_VariantId] ON [DON_HANG_CT] ([VariantId]);
GO


CREATE INDEX [IX_GIO_HANG_CT_VariantId] ON [GIO_HANG_CT] ([VariantId]);
GO


CREATE UNIQUE INDEX [IX_HANG_Name] ON [HANG] ([Name]);
GO


CREATE UNIQUE INDEX [IX_KHACH_HANG_Email] ON [KHACH_HANG] ([Email]) WHERE [Email] IS NOT NULL;
GO


CREATE UNIQUE INDEX [IX_KHACH_HANG_Phone] ON [KHACH_HANG] ([Phone]);
GO


CREATE INDEX [IX_LICH_SU_DON_ActorId] ON [LICH_SU_DON] ([ActorId]);
GO


CREATE INDEX [IX_LICH_SU_DON_OrderId_At] ON [LICH_SU_DON] ([OrderId], [At]);
GO


CREATE UNIQUE INDEX [IX_NHA_CUNG_CAP_Code] ON [NHA_CUNG_CAP] ([Code]);
GO


CREATE INDEX [IX_NHAT_KY_KHO_ActorId] ON [NHAT_KY_KHO] ([ActorId]);
GO


CREATE UNIQUE INDEX [IX_NHAT_KY_KHO_OperationKey_VariantId] ON [NHAT_KY_KHO] ([OperationKey], [VariantId]);
GO


CREATE UNIQUE INDEX [IX_NHAT_KY_KHO_OrderId_VariantId_Kind] ON [NHAT_KY_KHO] ([OrderId], [VariantId], [Kind]) WHERE [OrderId] IS NOT NULL;
GO


CREATE INDEX [IX_NHAT_KY_KHO_PurchaseId] ON [NHAT_KY_KHO] ([PurchaseId]);
GO


CREATE INDEX [IX_NHAT_KY_KHO_VariantId_At] ON [NHAT_KY_KHO] ([VariantId], [At]);
GO


CREATE INDEX [IX_PHIEN_AccountId] ON [PHIEN] ([AccountId]);
GO


CREATE INDEX [IX_PHIEN_ExpiresAt] ON [PHIEN] ([ExpiresAt]);
GO


CREATE UNIQUE INDEX [IX_PHIEU_NHAP_Code] ON [PHIEU_NHAP] ([Code]);
GO


CREATE INDEX [IX_PHIEU_NHAP_ConfirmerId] ON [PHIEU_NHAP] ([ConfirmerId]);
GO


CREATE INDEX [IX_PHIEU_NHAP_CreatorId] ON [PHIEU_NHAP] ([CreatorId]);
GO


CREATE INDEX [IX_PHIEU_NHAP_SupplierId] ON [PHIEU_NHAP] ([SupplierId]);
GO


CREATE UNIQUE INDEX [IX_PHIEU_NHAP_CT_PurchaseId_VariantId] ON [PHIEU_NHAP_CT] ([PurchaseId], [VariantId]);
GO


CREATE INDEX [IX_PHIEU_NHAP_CT_VariantId] ON [PHIEU_NHAP_CT] ([VariantId]);
GO


CREATE INDEX [IX_SAN_PHAM_BrandId_CategoryId] ON [SAN_PHAM] ([BrandId], [CategoryId]);
GO


CREATE INDEX [IX_SAN_PHAM_CategoryId] ON [SAN_PHAM] ([CategoryId]);
GO


CREATE UNIQUE INDEX [IX_SAN_PHAM_Code] ON [SAN_PHAM] ([Code]);
GO


CREATE UNIQUE INDEX [IX_TAI_KHOAN_Username] ON [TAI_KHOAN] ([Username]);
GO


CREATE INDEX [IX_THANH_TOAN_ActorId] ON [THANH_TOAN] ([ActorId]);
GO


