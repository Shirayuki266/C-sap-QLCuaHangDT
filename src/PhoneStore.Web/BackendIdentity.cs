using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using PhoneStore.Infrastructure.Data;
namespace PhoneStore.Web;
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
