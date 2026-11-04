using System.Data;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace PhoneStore.Infrastructure.Data;

public sealed class DatabaseMigrator(StoreDbContext db)
{
    public const string MigrationId = "rebuild/001_initial_store";
    private static readonly string Script = LoadScript();
    private static readonly string Hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Script)));

    private static string LoadScript()
    {
        using var stream = typeof(DatabaseMigrator).Assembly.GetManifestResourceStream("PhoneStore.Infrastructure.Migrations.001_InitialStore.sql")
            ?? throw new InvalidOperationException("Missing embedded schema migration.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    public async Task MigrateAsync(CancellationToken cancellationToken = default)
    {
        var target = new SqlConnectionStringBuilder(SqlTarget.Validate(db.Database.GetConnectionString()));
        var masterTarget = new SqlConnectionStringBuilder(target.ConnectionString) { InitialCatalog = "master" };
        await using (var master = new SqlConnection(masterTarget.ConnectionString))
        {
            await master.OpenAsync(cancellationToken);
            await using var create = master.CreateCommand();
            create.CommandTimeout = 60;
            create.CommandText = """
                DECLARE @result int;
                EXEC @result=sys.sp_getapplock @Resource=@lock, @LockMode='Exclusive', @LockOwner='Session', @LockTimeout=30000;
                IF @result<0 THROW 51000,N'Không thể khóa tạo CSDL.',1;
                BEGIN TRY
                    IF DB_ID(@name) IS NULL
                    BEGIN
                        DECLARE @sql nvarchar(max)=N'CREATE DATABASE '+QUOTENAME(@name);
                        EXEC(@sql);
                    END;
                END TRY
                BEGIN CATCH
                    EXEC sys.sp_releaseapplock @Resource=@lock,@LockOwner='Session';
                    THROW;
                END CATCH;
                EXEC sys.sp_releaseapplock @Resource=@lock,@LockOwner='Session';
                """;
            create.Parameters.AddWithValue("@name", target.InitialCatalog);
            create.Parameters.AddWithValue("@lock", "PhoneStore:create:" + target.InitialCatalog);
            await create.ExecuteNonQueryAsync(cancellationToken);
        }

        await db.Database.OpenConnectionAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        try
        {
            await db.Database.ExecuteSqlRawAsync("""
                DECLARE @result int;
                EXEC @result=sys.sp_getapplock @Resource=N'PhoneStore:rebuild:migrations',@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=30000;
                IF @result<0 THROW 51000,N'Không thể khóa migration.',1;
                """, cancellationToken);
            await using var command = ((SqlConnection)db.Database.GetDbConnection()).CreateCommand();
            command.Transaction = (SqlTransaction)transaction.GetDbTransaction();
            command.CommandText = "SELECT COUNT(*) FROM sys.tables WHERE is_ms_shipped=0";
            var tableCount = (int)(await command.ExecuteScalarAsync(cancellationToken))!;
            command.CommandText = "SELECT OBJECT_ID(N'dbo.__PhoneStoreMigrations',N'U')";
            var ledger = await command.ExecuteScalarAsync(cancellationToken);
            if ((ledger is null or DBNull) && tableCount != 0)
                throw new InvalidOperationException("CSDL đã có dữ liệu hoặc schema không thuộc bản xây lại; migration bị từ chối.");

            await db.Database.ExecuteSqlRawAsync("""
                IF OBJECT_ID(N'dbo.__PhoneStoreMigrations',N'U') IS NULL
                    CREATE TABLE dbo.__PhoneStoreMigrations (
                        Id nvarchar(200) NOT NULL PRIMARY KEY,
                        Sha256 char(64) NOT NULL,
                        AppliedAt datetime2(7) NOT NULL DEFAULT SYSUTCDATETIME());
                """, cancellationToken);
            command.CommandText = "SELECT Id,Sha256 FROM dbo.__PhoneStoreMigrations";
            var applied = false;
            await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
            {
                while (await reader.ReadAsync(cancellationToken))
                {
                    if (reader.GetString(0) != MigrationId || reader.GetString(1) != Hash)
                        throw new InvalidOperationException("CSDL thuộc phiên bản khác hoặc checksum migration đã thay đổi.");
                    applied = true;
                }
            }
            if (!applied)
            {
                if (tableCount > 1) throw new InvalidOperationException("Schema có bảng không được quản lý; migration bị từ chối.");
                await db.Database.ExecuteSqlRawAsync(Script, cancellationToken);
                await db.Database.ExecuteSqlInterpolatedAsync($"INSERT dbo.__PhoneStoreMigrations(Id,Sha256) VALUES({MigrationId},{Hash})", cancellationToken);
            }
            await transaction.CommitAsync(cancellationToken);
        }
        finally { await db.Database.CloseConnectionAsync(); }
    }

    public async Task<bool> IsCurrentAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await db.Database.SqlQuery<int>($"SELECT COUNT(*) AS [Value] FROM dbo.__PhoneStoreMigrations WHERE Id={MigrationId} AND Sha256={Hash} AND (SELECT COUNT(*) FROM dbo.__PhoneStoreMigrations)=1")
                .SingleAsync(cancellationToken) == 1;
        }
        catch (SqlException) { return false; }
    }
}
