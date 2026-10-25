using Microsoft.EntityFrameworkCore.Storage;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
namespace PhoneStore.Infrastructure.Data;
public static class DatabaseSetup
{
    public static async Task Migrate(StoreDbContext db)
    {
        var cs=new SqlConnectionStringBuilder(db.Database.GetConnectionString());
        var database=cs.InitialCatalog;
        if(string.IsNullOrWhiteSpace(database)||database is "master" or "model" or "msdb" or "tempdb") throw new InvalidOperationException("Cần CSDL riêng cho PhoneStore.");
        cs.InitialCatalog="master";
        await using(var master=new SqlConnection(cs.ConnectionString)) {
            await master.OpenAsync(); await using var create=master.CreateCommand();
            create.CommandText="IF DB_ID(@name) IS NULL BEGIN DECLARE @sql nvarchar(max)=N'CREATE DATABASE '+QUOTENAME(@name); EXEC(@sql); END";
            create.Parameters.AddWithValue("@name",database); await create.ExecuteNonQueryAsync();
        }
        await db.Database.OpenConnectionAsync();
        await using var tx=await db.Database.BeginTransactionAsync();
        await db.Database.ExecuteSqlRawAsync("DECLARE @r int; EXEC @r=sp_getapplock @Resource=N'PhoneStore:migrations',@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=30000; IF @r<0 THROW 51000,N'Migration đang chạy',1; IF OBJECT_ID(N'__PhoneStoreMigrations') IS NULL CREATE TABLE __PhoneStoreMigrations (Id nvarchar(200) NOT NULL PRIMARY KEY, Sha256 varchar(64) NOT NULL, AppliedAt datetime2 NOT NULL DEFAULT SYSUTCDATETIME());");
        var assembly=typeof(DatabaseSetup).Assembly;
        foreach(var resource in assembly.GetManifestResourceNames().Where(x=>x.Contains(".Migrations.")&&x.EndsWith(".sql")).Order()) {
            using var reader=new StreamReader(assembly.GetManifestResourceStream(resource)!); var sql=await reader.ReadToEndAsync();
            var hash=Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(sql)));
            var connection=(SqlConnection)db.Database.GetDbConnection();
            await using var check=connection.CreateCommand(); check.Transaction=(SqlTransaction)tx.GetDbTransaction();
            check.CommandText="SELECT Sha256 FROM __PhoneStoreMigrations WHERE Id=@id"; check.Parameters.AddWithValue("@id",resource);
            var previous=await check.ExecuteScalarAsync(); if(previous!=null) {if((string)previous!=hash) throw new InvalidOperationException("Migration đã áp dụng bị thay đổi: "+resource);continue;}
            foreach(var batch in Regex.Split(sql,@"^\s*GO\s*$",RegexOptions.Multiline|RegexOptions.IgnoreCase)) if(!string.IsNullOrWhiteSpace(batch)) await db.Database.ExecuteSqlRawAsync(batch);
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO __PhoneStoreMigrations(Id,Sha256) VALUES({resource},{hash})");
        }
        await tx.CommitAsync();
    }
}


