using System.Net;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using PhoneStore.Web;
namespace PhoneStore.Checks;
internal static class ReadinessIdentityChecks
{
    public static async Task Run(HttpClient client,string? connection)
    {
        await Check.Run(connection is null?"BASE.08 A01.04 not-ready target remains 503 without configuration leaks":"BASE.01 A01.04 readiness exposes stable environment and application-target identity",async()=>
        {
            using var response=await client.GetAsync("/health/ready");var text=await response.Content.ReadAsStringAsync();using var json=JsonDocument.Parse(text);
            if(connection is null){Check.True(response.StatusCode==HttpStatusCode.ServiceUnavailable&&json.RootElement.GetProperty("code").GetString()=="DATABASE_UNAVAILABLE"&&!text.Contains("Password="),"Not-ready identity leaked configuration or became successful.");return;}
            Check.True(response.IsSuccessStatusCode&&json.RootElement.GetProperty("environment").GetString()=="Testing"&&json.RootElement.GetProperty("fingerprint").GetString()==BackendIdentity.Fingerprint(connection,"Testing"),"Readiness target metadata incorrect.");
            var target=new SqlConnectionStringBuilder(connection);Check.True(!text.Contains(target.InitialCatalog,StringComparison.Ordinal)&&!text.Contains("Password="),"Ready response exposed raw SQL target/configuration.");
        });
        if(connection is not null)await Check.Run("A01.04 readiness identity varies by environment/target and excludes all credentials",()=>
        {
            const string first="Server=AuditServer;Database=AuditStore;User ID=first;Password=DoNotExpose123!";
            const string second="Server=AuditServer;Database=AuditStore;User ID=second;Password=DifferentSecret123!;Encrypt=True";
            var fingerprint=BackendIdentity.Fingerprint(first,"Testing");
            Check.True(fingerprint.Length==64&&fingerprint.All(Uri.IsHexDigit)&&fingerprint==BackendIdentity.Fingerprint(second,"Testing")&&fingerprint!=BackendIdentity.Fingerprint(first,"Development")&&fingerprint!=BackendIdentity.Fingerprint(first.Replace("AuditStore","OtherStore",StringComparison.Ordinal),"Testing"),"Target identity mixed secrets or failed to distinguish environments/targets.");
            return Task.CompletedTask;
        });
    }
}
