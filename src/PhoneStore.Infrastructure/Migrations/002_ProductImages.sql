CREATE TABLE SAN_PHAM_ANH (
 Id bigint IDENTITY(1,1) NOT NULL PRIMARY KEY,
 ProductId bigint NOT NULL,
 Url nvarchar(1000) NOT NULL,
 SortOrder int NOT NULL,
 CONSTRAINT FK_ProductImages_Product FOREIGN KEY(ProductId) REFERENCES SAN_PHAM(Id),
 CONSTRAINT CK_ProductImages_Order CHECK(SortOrder>=0)
);
CREATE INDEX IX_ProductImages_Product_Sort ON SAN_PHAM_ANH(ProductId,SortOrder);
