using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PhoneStore.Core.Accounts;
using PhoneStore.Infrastructure.Data;

namespace PhoneStore.Checks;

internal static class RegistrationHttpChecks
{
    public static async Task Run(HttpClient client, string connection)
    {
        StoreDbContext Open() => new(new DbContextOptionsBuilder<StoreDbContext>().UseSqlServer(connection).Options);
        var input = new RegistrationRequest("http-register", "Registration@12345", "Khách HTTP", "0991234560", "http-register@example.test");
        using var tokenResponse = await client.GetAsync("/api/security/token");
        Check.True(tokenResponse.IsSuccessStatusCode, "Anti-forgery token endpoint failed.");
        using var tokenJson = JsonDocument.Parse(await tokenResponse.Content.ReadAsStringAsync());
        var token = tokenJson.RootElement.GetProperty("token").GetString()!;
        async Task<HttpResponseMessage> Send(object body, string? csrf)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/register") { Content = JsonContent.Create(body) };
            if (csrf is not null) request.Headers.Add("X-CSRF-TOKEN", csrf);
            return await client.SendAsync(request);
        }
        await Check.Run("K01 HTTP rejects missing/invalid CSRF without writing", async () =>
        {
            foreach (var csrf in new string?[] { null, "invalid" })
            {
                using var response = await Send(input, csrf);
                using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                Check.True(response.StatusCode == HttpStatusCode.BadRequest && json.RootElement.GetProperty("code").GetString() == "INVALID_INPUT", "CSRF was not safely rejected.");
            }
            using var otherClient = new HttpClient(new HttpClientHandler { UseProxy = false }) { BaseAddress = client.BaseAddress };
            using var otherCookie = await otherClient.GetAsync("/api/security/token");
            using var foreignTokenRequest = new HttpRequestMessage(HttpMethod.Post, "/api/auth/register") { Content = JsonContent.Create(input) };
            foreignTokenRequest.Headers.Add("X-CSRF-TOKEN", token);
            using var foreignTokenResponse = await otherClient.SendAsync(foreignTokenRequest);
            Check.True(foreignTokenResponse.StatusCode == HttpStatusCode.BadRequest, "Anti-forgery token from another cookie was accepted.");
            await using var db = Open();
            Check.True(!await db.Accounts.AnyAsync(x => x.Username == input.Username), "Rejected CSRF created an account.");
        });
        await Check.Run("K01 HTTP creates CUSTOMER with safe response and no login cookie/session", async () =>
        {
            await using var before = Open(); var sessions = await before.Sessions.CountAsync();
            using var response = await Send(input, token);
            var body = await response.Content.ReadAsStringAsync();
            using var json = JsonDocument.Parse(body);
            Check.True(response.StatusCode == HttpStatusCode.Created && json.RootElement.GetProperty("role").GetString() == "CUSTOMER", "Registration HTTP success contract failed.");
            Check.True(!body.Contains(input.Password!) && !body.Contains("passwordHash", StringComparison.OrdinalIgnoreCase), "Registration response exposed credentials.");
            Check.True(!response.Headers.TryGetValues("Set-Cookie", out _), "Registration unexpectedly issued a login cookie.");
            await using var db = Open();
            var account = await db.Accounts.SingleAsync(x => x.Username == input.Username);
            Check.True(await db.Customers.AnyAsync(x => x.Id == account.Id) && await db.Sessions.CountAsync() == sessions, "Registration profile/session invariant failed.");
        });
        await Check.Run("K01 HTTP duplicate/invalid/overposted JSON has safe structured errors", async () =>
        {
            using var duplicate = await Send(input, token);
            using var duplicateJson = JsonDocument.Parse(await duplicate.Content.ReadAsStringAsync());
            Check.True(duplicate.StatusCode == HttpStatusCode.BadRequest && duplicateJson.RootElement.GetProperty("code").GetString() == "INVALID_INPUT", "Duplicate response contract failed.");
            using var invalid = await Send(input with { Username = "invalid-input", Phone = "bad" }, token);
            Check.True(invalid.StatusCode == HttpStatusCode.BadRequest, "Invalid profile was accepted.");
            using var missingFields = await Send(new { }, token);
            Check.True(missingFields.StatusCode == HttpStatusCode.BadRequest, "Missing registration fields were accepted.");
            using var elevated = await Send(new { username = "injected-admin", password = input.Password, fullName = input.FullName, phone = "0991234561", role = "ADMIN", passwordHash = "fake" }, token);
            Check.True(elevated.StatusCode == HttpStatusCode.BadRequest, "Unknown privilege/hash fields were accepted.");
            using var malformedRequest = new HttpRequestMessage(HttpMethod.Post, "/api/auth/register") { Content = new StringContent("{invalid", Encoding.UTF8, "application/json") };
            malformedRequest.Headers.Add("X-CSRF-TOKEN", token);
            using var malformed = await client.SendAsync(malformedRequest);
            using var malformedJson = JsonDocument.Parse(await malformed.Content.ReadAsStringAsync());
            Check.True(malformed.StatusCode == HttpStatusCode.BadRequest && malformedJson.RootElement.GetProperty("code").GetString() == "INVALID_INPUT", "Malformed JSON did not produce safe validation error.");
            await using var db = Open();
            Check.True(!await db.Accounts.AnyAsync(x => x.Username == "injected-admin" || x.Username == "invalid-input"), "Rejected request wrote an account.");
        });
    }
}
