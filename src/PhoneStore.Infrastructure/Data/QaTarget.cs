using Microsoft.Data.SqlClient;
using System.Text.RegularExpressions;
namespace PhoneStore.Infrastructure.Data;
/// <summary>Fail closed before any QA migration, seed or write.</summary>
public static class QaTarget
{
    public static void Validate(string? connection, string? expectedServer, string? expectedDatabase)
    {
        if(string.IsNullOrWhiteSpace(connection)||string.IsNullOrWhiteSpace(expectedServer)||string.IsNullOrWhiteSpace(expectedDatabase))
            throw new InvalidOperationException("QA yêu cầu connection, server và database rõ ràng; không fallback về PhoneStore.");
        var cs=new SqlConnectionStringBuilder(connection);
        if(!Regex.IsMatch(expectedDatabase,@"^PhoneStore_QA(?:_[A-Za-z0-9_]+)?$",RegexOptions.CultureInvariant)||
           !string.Equals(cs.InitialCatalog,expectedDatabase,StringComparison.OrdinalIgnoreCase)||
           !string.Equals(cs.DataSource,expectedServer,StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Đích QA không khớp server/database cấu hình hoặc không phải CSDL PhoneStore_QA.");
    }
    public static async Task VerifyHttp(string url)
    {
        var server=Environment.GetEnvironmentVariable("PHONESTORE_QA_SERVER");
        var database=Environment.GetEnvironmentVariable("PHONESTORE_QA_DATABASE");
        Validate(Environment.GetEnvironmentVariable("PHONESTORE_TEST_CONNECTION"),server,database);
        if(!Uri.TryCreate(url,UriKind.Absolute,out var uri)||!uri.IsLoopback)
            throw new InvalidOperationException("QA URL phải là loopback được cấu hình rõ ràng.");
        using var client=new HttpClient(new HttpClientHandler{UseProxy=false});
        using var response=await client.GetAsync(new Uri(uri,"/api/qa/identity"));response.EnsureSuccessStatusCode();
        using var json=System.Text.Json.JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var identity=json.RootElement;
        if(identity.GetProperty("environment").GetString()!="QA"||identity.GetProperty("database").GetString()!=database||identity.GetProperty("server").GetString()!=server)
            throw new InvalidOperationException("Backend không khớp đích QA; dừng trước mọi request ghi.");
    }
}
