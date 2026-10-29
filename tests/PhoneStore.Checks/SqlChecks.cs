using Microsoft.Data.SqlClient;
using PhoneStore.Infrastructure.Data;

namespace PhoneStore.Checks;

internal static class SqlChecks
{
    public static async Task Run()
    {
        var configured = SqlTarget.Validate(Environment.GetEnvironmentVariable("PHONESTORE_TEST_CONNECTION"));
        var target = new SqlConnectionStringBuilder(configured);
        Check.True(target.InitialCatalog.StartsWith("PhoneStore_QA_", StringComparison.Ordinal), "Integration checks require a PhoneStore_QA_ target.");
        Check.True(target.DataSource == Environment.GetEnvironmentVariable("PHONESTORE_QA_SERVER") &&
            target.InitialCatalog == Environment.GetEnvironmentVariable("PHONESTORE_QA_DATABASE"), "Integration target does not match the explicit QA environment.");
        var database = "PhoneStore_QA_Foundation_" + Guid.NewGuid().ToString("N");
        var masterTarget = new SqlConnectionStringBuilder(target.ConnectionString) { InitialCatalog = "master" };
        await using var master = new SqlConnection(masterTarget.ConnectionString);
        await master.OpenAsync();
        await using (var create = master.CreateCommand())
        {
            create.CommandText = $"CREATE DATABASE [{database}]";
            await create.ExecuteNonQueryAsync();
        }
        try
        {
            target.InitialCatalog = database;
            await using var sql = new SqlConnection(target.ConnectionString);
            await sql.OpenAsync();
            await Check.Run("SQL integration uses its own real SQL Server database", async () =>
            {
                await using var query = sql.CreateCommand();
                query.CommandText = "SELECT DB_NAME()";
                Check.True((string?)await query.ExecuteScalarAsync() == database, "Wrong integration database.");
            });
            await Check.Run("SQL transaction rollback leaves no partially written data", async () =>
            {
                await using (var setup = sql.CreateCommand())
                {
                    setup.CommandText = "CREATE TABLE dbo.FoundationProbe (Id int NOT NULL PRIMARY KEY)";
                    await setup.ExecuteNonQueryAsync();
                }
                await using (var transaction = (SqlTransaction)await sql.BeginTransactionAsync())
                {
                    await using var write = sql.CreateCommand();
                    write.Transaction = transaction;
                    write.CommandText = "INSERT dbo.FoundationProbe (Id) VALUES (1)";
                    await write.ExecuteNonQueryAsync();
                    await transaction.RollbackAsync();
                }
                await using var count = sql.CreateCommand();
                count.CommandText = "SELECT COUNT(*) FROM dbo.FoundationProbe";
                Check.True((int)(await count.ExecuteScalarAsync())! == 0, "Rollback left a partial write.");
            });
        }
        finally
        {
            SqlConnection.ClearAllPools();
            await using var cleanup = master.CreateCommand();
            // The identifier is generated above, never supplied by configuration or user input.
            cleanup.CommandText = $"DROP DATABASE [{database}]";
            await cleanup.ExecuteNonQueryAsync();
        }
    }
}
