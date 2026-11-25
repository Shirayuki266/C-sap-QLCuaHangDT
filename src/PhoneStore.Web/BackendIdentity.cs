using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using PhoneStore.Infrastructure.Data;
namespace PhoneStore.Web;
// [UC05.01] Sinh mã vân tay SHA-256 (environment + SQL server + database) để WinForms kiểm tra đúng backend đích.
public static class BackendIdentity
{
    public static string Fingerprint(string connection,string environment)
    {
        var target=new SqlConnectionStringBuilder(SqlTarget.Validate(connection));
        // This identifies the configured application target, not its TLS certificate.
        // Credentials and transport options never participate in the public digest.
        var identity=new{Application="PhoneStore",Environment=environment,Server=target.DataSource.Trim(),Database=target.InitialCatalog.Trim()};
        return Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(identity)));
    }
}
