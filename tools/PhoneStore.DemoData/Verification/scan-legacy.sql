-- Read-only business-data audit. The caller supplies @terms as a JSON string array
-- from docs/demo-data/legacy-denylist.json (classification = hard).
-- Temporary tables contain column names/counts only; no cell values or secrets.
SET NOCOUNT ON;
IF DB_NAME() NOT LIKE N'PhoneStore[_]Demo[_]%' AND DB_NAME() NOT LIKE N'PhoneStore[_]QA[_]%'
    THROW 51000,N'Demo/QA database required',1;
IF ISJSON(@terms)<>1 OR NOT EXISTS(SELECT 1 FROM OPENJSON(@terms))
    THROW 51000,N'An explicit nonempty denylist is required',1;
CREATE TABLE #columns(TableName nvarchar(300),ColumnName nvarchar(128),MatchedRows bigint);
DECLARE @sql nvarchar(max)=N'';
SELECT @sql+=N'INSERT #columns SELECT N'''+REPLACE(s.name+N'.'+t.name,N'''',N'''''')+N''',N'''+REPLACE(c.name,N'''',N'''''')+
    N''',COUNT_BIG(*) FROM '+QUOTENAME(s.name)+N'.'+QUOTENAME(t.name)+N' r WHERE EXISTS(SELECT 1 FROM OPENJSON(@terms) d WHERE CHARINDEX(CONVERT(nvarchar(max),d.value),CONVERT(nvarchar(max),r.'+QUOTENAME(c.name)+N'))>0);'
FROM sys.tables t JOIN sys.schemas s ON s.schema_id=t.schema_id
JOIN sys.columns c ON c.object_id=t.object_id JOIN sys.types ty ON ty.user_type_id=c.user_type_id
WHERE ty.name IN(N'varchar',N'nvarchar',N'char',N'nchar',N'text',N'ntext');
EXEC sys.sp_executesql @sql,N'@terms nvarchar(max)',@terms;
SELECT DB_NAME() AS [database],(SELECT COUNT(*) FROM sys.tables) AS tables,
    (SELECT COUNT(*) FROM #columns) AS stringColumns,
    (SELECT COUNT(*) FROM OPENJSON(@terms)) AS suppliedTerms,
    JSON_QUERY((SELECT TableName,ColumnName,MatchedRows FROM #columns ORDER BY TableName,ColumnName FOR JSON PATH)) AS columns,
    (SELECT COUNT_BIG(*) FROM dbo.DON_HANG_CT l JOIN dbo.BIEN_THE v ON v.Id=l.VariantId
        JOIN dbo.SAN_PHAM p ON p.Id=v.ProductId WHERE l.Name<>p.Name OR l.Sku<>v.Sku) AS snapshotMismatches
FOR JSON PATH,WITHOUT_ARRAY_WRAPPER;
DROP TABLE #columns;
