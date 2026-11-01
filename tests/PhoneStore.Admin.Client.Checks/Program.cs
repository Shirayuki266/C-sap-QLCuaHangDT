using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PhoneStore.Admin.Client;
using PhoneStore.Admin.Client.Checks;

if(args.Contains("--desktop-smoke")){await DesktopQaChecks.Run();return;}
if(args.Contains("--desktop-catalog")){await DesktopQaChecks.Run(catalog:true);return;}

var passed = 0;
void Assert(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
async Task Error(string expected, Func<Task> action) { try { await action(); throw new InvalidOperationException("Expected " + expected); } catch (AdminApiException error) { Assert(error.Code == expected, "Unexpected safe error: " + error.Code); } }
await using var qa = await QaBackend.Create();
await using var baselineDb = qa.Open();
async Task<string> Commerce()
{
    // Every business table remains empty in W1; sessions/accounts are the only permitted mutations.
    var counts = new List<int>(); foreach (var table in new[] { "SAN_PHAM", "BIEN_THE", "TON_KHO", "DON_HANG", "DON_HANG_CT", "THANH_TOAN", "LICH_SU_DON", "NHAT_KY_KHO", "PHIEU_NHAP", "PHIEU_NHAP_CT" })
    { await using var command = baselineDb.Database.GetDbConnection().CreateCommand(); command.CommandText = $"SELECT COUNT(*) FROM dbo.[{table}]"; if (command.Connection!.State != System.Data.ConnectionState.Open) await command.Connection.OpenAsync(); counts.Add(Convert.ToInt32(await command.ExecuteScalarAsync())); }
    return string.Join(',', counts);
}
var initialCommerce = await Commerce();
var sqlTrace = new List<object>();
async Task<object> Snapshot() => new
{
    accounts = await baselineDb.Accounts.CountAsync(), sessions = await baselineDb.Sessions.CountAsync(),
    orders = await baselineDb.Orders.CountAsync(), total = await baselineDb.Orders.SumAsync(x => (decimal?)x.Total) ?? 0,
    inventoryRows = await baselineDb.Inventory.CountAsync(), H = await baselineDb.Inventory.SumAsync(x => (long?)x.OnHand) ?? 0,
    R = await baselineDb.Inventory.SumAsync(x => (long?)x.Reserved) ?? 0, payments = await baselineDb.Payments.CountAsync(),
    history = await baselineDb.OrderHistory.CountAsync(), journal = await baselineDb.Movements.CountAsync()
};
async Task Test(string name, Func<Task> action)
{
    var before = await Snapshot(); await action(); var after = await Snapshot();
    sqlTrace.Add(new { name, status = "PASS", before, after }); Console.WriteLine("PASS " + name); passed++;
}
await Test("WIN.BASE.05 URL policy blocks remoteHTTP credentials path query fragment; permits HTTPS and loopback", async () =>
{
    foreach (var address in new[] { "http://remote.example", "https://user:pass@example.test", "https://example.test/sub", "https://example.test?x=1", "file:///tmp/a", "//example.test", "https://example.test/#x" }) await Error("INVALID_SERVER", () => { ServerAddress.Validate(address); return Task.CompletedTask; });
    foreach (var address in new[] { "http://localhost:5080", "http://127.0.0.1:5080", "http://[::1]:5080", "https://example.test" }) ServerAddress.Validate(address);
});
await Test("WIN.A01.01 actual readiness fingerprint; expected mismatch prevents login request", async () =>
{
    using var api = qa.Api(); var ready = await api.ReadinessAsync(); Assert(ready.Environment == "Testing" && ready.Fingerprint.Length > 8, "Readiness missing public target identity");
    var probe = new Probe(); using var wrong = qa.Api(probe, new("https://localhost", "Production")); await Error("SERVER_MISMATCH", () => wrong.LoginAsync("admin-w1-qa", qa.Password)); Assert(probe.Posts == 0, "Mismatch sent credentials");
    using var fingerprint = qa.Api(settings: new("https://localhost", "Testing", "wrong-target")); await Error("SERVER_MISMATCH", () => fingerprint.LoginAsync("admin-w1-qa", qa.Password));
});
await Test("WIN.A01.02 invalid CUSTOMER locked login create no session and use generic feedback", async () =>
{
    var before = await baselineDb.Sessions.CountAsync(); foreach (var (username, password) in new[] { ("missing-w1-qa", qa.Password), ("admin-w1-qa", "wrong"), ("customer-w1-qa", qa.Password), ("locked-w1-qa", qa.Password) })
    { using var api = qa.Api(); await Error("LOGIN_FAILED", () => api.LoginAsync(username, password)); Assert(api.Identity is null, "Rejected login retained identity"); }
    Assert(await baselineDb.Sessions.CountAsync() == before, "Rejected login left a partial session");
});
await Test("WIN.BASE.04 actual missing/wrong CSRF400 and per-write token refresh with same session cookie", async () =>
{
    using var raw = qa.CreateClient(new() { BaseAddress = new("https://localhost"), HandleCookies = false, AllowAutoRedirect = false });
    using var missing = await raw.PostAsJsonAsync("/api/auth/login", new LoginInput("admin-w1-qa", qa.Password)); Assert(missing.StatusCode == HttpStatusCode.BadRequest, "Missing CSRF accepted");
    var probe = new Probe(); using var api = qa.Api(probe); await api.LoginAsync("admin-w1-qa", qa.Password); await api.LogoutAsync();
    Assert(probe.Posts == 2 && probe.TokenGets == 2 && probe.WritesWithoutToken == 0, "Fresh per-command CSRF absent");
    var dropped = new Probe { RemoveToken = true }; using var bad = qa.Api(dropped); await Error("INVALID_INPUT", () => bad.LoginAsync("admin-w1-qa", qa.Password)); Assert(dropped.Posts == 1 && bad.Identity is null, "CSRF failure retried blindly");
});
await Test("WIN.BASE.07 expired or revoked SQL session401 clears client; subsequent command sends no POST", async () =>
{
    var probe = new Probe(); using var api = qa.Api(probe); await api.LoginAsync("admin-w1-qa", qa.Password); var ended = 0; api.SessionEnded += () => ended++;
    await baselineDb.Sessions.Where(x => x.AccountId == qa.AdminId).ExecuteUpdateAsync(s => s.SetProperty(x => x.ExpiresAt, DateTime.UtcNow.AddMinutes(-1)));
    await Error("UNAUTHORIZED", () => api.GetAsync<AdminIdentity>("/api/auth/session")); var posts = probe.Posts;
    await Error("UNAUTHORIZED", () => api.ChangePasswordAsync(qa.Password, qa.NextPassword)); Assert(probe.Posts == posts && api.Identity is null && ended == 1, "Expired session continued writing");
});
await Test("WIN.A01.04 lost login response requires session GET before explicit retry, never stuck", async () =>
{
    var probe = new Probe { LoseLoginResponse = true };using var api = qa.Api(probe);
    await Error("UNCERTAIN",()=>api.LoginAsync("admin-w1-qa",qa.Password));var writes=probe.Posts;
    await Error("UNCERTAIN",()=>api.LoginAsync("admin-w1-qa",qa.Password));Assert(probe.Posts==writes,"Login retried before reconciliation");
    await api.RecoverSessionAsync();Assert(!api.NeedsReconciliation&&api.Identity is null,"Session GET did not restore retry path");
    probe.LoseLoginResponse=false;await api.LoginAsync("admin-w1-qa",qa.Password);await api.LogoutAsync();
});
await Test("WIN.BASE.06 malformed successful login JSON reconciles received cookie instead of replaying credentials",async()=>
{
    var probe=new Probe{CorruptLoginResponse=true};using var api=qa.Api(probe);
    await Error("UNCERTAIN",()=>api.LoginAsync("admin-w1-qa",qa.Password));var posts=probe.Posts;
    var recovered=await api.RecoverSessionAsync();Assert(recovered?.Role=="ADMIN"&&probe.Posts==posts,"Malformed JSON recovery replayed write or lost session");await api.LogoutAsync();
});
await Test("WIN.A03 actual invalid password leaves hash/sessions; valid change revokes every session", async () =>
{
    using var first = qa.Api(); using var second = qa.Api(); await first.LoginAsync("admin-w1-qa", qa.Password); await second.LoginAsync("admin-w1-qa", qa.Password);
    var oldHash = await baselineDb.Accounts.Where(x => x.Id == qa.AdminId).Select(x => x.PasswordHash).SingleAsync();
    foreach (var values in new[] { ("wrong", qa.NextPassword), (qa.Password, "tiny"), (qa.Password, new string('x', 129)) }) await Error("INVALID_INPUT", () => first.ChangePasswordAsync(values.Item1, values.Item2));
    Assert(oldHash == await baselineDb.Accounts.Where(x => x.Id == qa.AdminId).Select(x => x.PasswordHash).SingleAsync(), "Invalid password changed hash");
    await first.ChangePasswordAsync(qa.Password, qa.NextPassword); Assert(first.Identity is null && !await baselineDb.Sessions.AnyAsync(x => x.AccountId == qa.AdminId), "Password failed to revoke all sessions");
    await Error("UNAUTHORIZED", () => second.GetAsync<AdminIdentity>("/api/auth/session"));
    using var renewed = qa.Api(); await Error("LOGIN_FAILED", () => renewed.LoginAsync("admin-w1-qa", qa.Password)); await renewed.LoginAsync("admin-w1-qa", qa.NextPassword);
});
await Test("WIN.A02 actual logout revokes session; repeated/expired logout is safe", async () =>
{
    var before = await baselineDb.Sessions.CountAsync(x => x.AccountId == qa.AdminId);
    using var api = qa.Api(); await api.LoginAsync("admin-w1-qa", qa.NextPassword); await api.LogoutAsync(); await api.LogoutAsync();
    Assert(api.Identity is null && await baselineDb.Sessions.CountAsync(x => x.AccountId == qa.AdminId) == before, "Logout did not revoke exactly its session");
    using var expired = qa.Api(); await expired.LoginAsync("admin-w1-qa", qa.NextPassword); await baselineDb.Sessions.Where(x => x.AccountId == qa.AdminId).ExecuteDeleteAsync(); await expired.LogoutAsync(); Assert(expired.Identity is null, "Expired logout failed local cleanup");
});
await Test("WIN.BASE.08 post-commit response loss blocks blind retry until authoritative GET reconciliation", async () =>
{
    var probe = new Probe(); using var api = qa.Api(probe); await api.LoginAsync("admin-w1-qa", qa.NextPassword); probe.LoseLogoutResponse = true;
    await Error("UNCERTAIN", () => api.CommandAsync("/api/auth/logout", new EmptyInput(), new CommandIntent())); var writes = probe.Posts;
    await Error("UNCERTAIN", () => api.CommandAsync("/api/auth/logout", new EmptyInput(), new CommandIntent())); Assert(probe.Posts == writes, "Unknown write blindly retried");
    await Error("NETWORK", () => api.ReconcileAsync(_ => throw new AdminApiException("NETWORK"))); Assert(api.NeedsReconciliation, "Failed reconciliation cleared guard");
    await Error("UNAUTHORIZED", () => api.ReconcileAsync(async ct => { await api.GetAsync<AdminIdentity>("/api/auth/session", ct); })); Assert(api.Identity is null, "Committed logout not reconciled");
});
await Test("WIN.BASE.11/12/20 typed request and page JSON, exact Vietnamese money and UTC+7, safe bounded query", async () =>
{
    using var api = qa.Api(); await api.LoginAsync("admin-w1-qa", qa.NextPassword); var page = await api.GetAsync<PageResult<JsonElement>>("/api/admin/suppliers" + AdminFormatting.ListQuery("no-qa-match"));
    Assert(page.Total == 0 && page.Items.Count == 0 && page.Page == 1 && page.PageSize == 20, "Page wrapper wrong");
    Assert(AdminFormatting.Money(23980000) == "23.980.000 đ" && AdminFormatting.Time(new DateTimeOffset(2026, 1, 1, 20, 0, 0, TimeSpan.Zero)) == "02/01/2026 03:00", "Money/time formatting wrong");
    await Error("INVALID_INPUT", () => { AdminFormatting.ListQuery(null, 1, 101); return Task.CompletedTask; });
    var json = JsonSerializer.Serialize(new PasswordInput("qa-current", "qa-next"), new JsonSerializerOptions(JsonSerializerDefaults.Web)); Assert(json == "{\"currentPassword\":\"qa-current\",\"newPassword\":\"qa-next\"}", "Command fields not exact");
    var intent = new CommandIntent(); Assert(intent.Key != Guid.Empty && intent.Key == intent.Key && new CommandIntent().Key != intent.Key, "Intent key unstable");
});
await Test("WIN.BASE.06 controlled errors for HTTP business codes500 trace JSON network timeout", async () =>
{
    foreach (var (code, status) in new[] { ("FORBIDDEN", 403), ("NOT_FOUND", 404), ("CONCURRENT_UPDATE", 409), ("INVALID_STATE", 409), ("OUT_OF_STOCK", 409), ("DATABASE_UNAVAILABLE", 503), ("SERVER_ERROR", 500) })
    { using var api = new ApiClient(new("https://localhost"), new StaticResponse(status, JsonSerializer.Serialize(new { code, message = "private-error-do-not-show", traceId = "qa-trace-1" }))); try { await api.ReadinessAsync(); } catch (AdminApiException error) { Assert(error.Status == status && !error.Message.Contains("private-error"), "Unsafe error displayed"); if (status == 500) Assert(error.Message.Contains("qa-trace-1"), "Trace absent"); } }
    using var invalid = new ApiClient(new("https://localhost"), new StaticResponse(200, "not-json")); await Error("INVALID_RESPONSE", () => invalid.ReadinessAsync());
    using var offline = new ApiClient(new("https://localhost"), new StaticResponse(0, "")); await Error("NETWORK", () => offline.ReadinessAsync());
    using var slow = new ApiClient(new("https://localhost"), new StaticResponse(-1, "")); await Error("TIMEOUT", () => slow.ReadinessAsync());
});
await Test("WIN.BASE.02/03 application architecture and secret boundary", () =>
{
    foreach (var project in new[] { "PhoneStore.Admin.Client", "PhoneStore.Admin" })
    {
        var directory = Path.Combine(QaBackend.Root, "src", project); var source = string.Join('\n', Directory.GetFiles(directory, "*.cs", SearchOption.TopDirectoryOnly).Select(File.ReadAllText));
        var projectXml = File.ReadAllText(Path.Combine(directory, project + ".csproj"));
        var assetsPath = Path.Combine(directory, "obj", "project.assets.json");
        var assets = File.Exists(assetsPath) ? File.ReadAllText(assetsPath) : project == "PhoneStore.Admin" && !OperatingSystem.IsWindows() ? "" : throw new InvalidOperationException("Application assets missing");
        foreach (var forbidden in new[] { "PhoneStore.Infrastructure", "PhoneStore.Web", "Microsoft.EntityFrameworkCore", "Microsoft.Data.SqlClient", "System.Data.SqlClient" }) Assert(!projectXml.Contains(forbidden) && !assets.Contains(forbidden) && !source.Contains(forbidden), "Forbidden application dependency: " + project);
        foreach (var forbidden in new[] { "Server=", "ConnectionString", qa.Password, qa.NextPassword }) Assert(!source.Contains(forbidden), "Secret/SQL in application");
    }
    lock(qa.LogMessages)Assert(!qa.LogMessages.Any(m => m.Contains(qa.Password) || m.Contains(qa.NextPassword)), "Backend logged submitted password"); return Task.CompletedTask;
});
Assert(initialCommerce == await Commerce(), "W1 mutated commerce/inventory/money/history"); Console.WriteLine("PASS SQL before/after: business tables unchanged; only account hash/session changes authorized");
Console.WriteLine($"{passed} admin client checks passed.");
var evidence = Path.Combine(QaBackend.Root, ".artifacts", "admin-w1"); Directory.CreateDirectory(evidence);
await File.WriteAllTextAsync(Path.Combine(evidence,"sql-results.json"),JsonSerializer.Serialize(new{qaOnly=true,checks=passed,allCommerceUnchanged=true,records=sqlTrace},new JsonSerializerOptions{WriteIndented=true}));
await CatalogChecks.Run();

internal sealed class Probe : DelegatingHandler
{
    public int Posts, TokenGets, WritesWithoutToken;
    public bool RemoveToken, LoseLogoutResponse, LoseLoginResponse, CorruptLoginResponse;
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.RequestUri!.AbsolutePath == "/api/security/token") TokenGets++;
        if (request.Method == HttpMethod.Post) { Posts++; if (RemoveToken) request.Headers.Remove("X-CSRF-TOKEN"); if (!request.Headers.Contains("X-CSRF-TOKEN")) WritesWithoutToken++; }
        var response = await base.SendAsync(request, cancellationToken);
        if(CorruptLoginResponse&&request.RequestUri.AbsolutePath=="/api/auth/login"&&response.IsSuccessStatusCode)response.Content=new StringContent("not-json",System.Text.Encoding.UTF8,"application/json");
        if (LoseLogoutResponse && request.RequestUri.AbsolutePath == "/api/auth/logout") { response.Dispose(); throw new HttpRequestException("QA response lost after server commit"); }
        if (LoseLoginResponse && request.RequestUri.AbsolutePath == "/api/auth/login") { response.Dispose(); throw new HttpRequestException("QA login response lost"); }
        return response;
    }
}
internal sealed class StaticResponse(int status, string json) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (status == 0) throw new HttpRequestException("QA network"); if (status == -1) throw new TaskCanceledException("QA timeout");
        return Task.FromResult(new HttpResponseMessage((HttpStatusCode)status) { Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json") });
    }
}
