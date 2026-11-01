using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace PhoneStore.Admin.Client;

public sealed class ApiClient : IAdminApi
{
    private readonly HttpClient http;
    private readonly CookieContainer cookies = new();
    private readonly ServerSettings settings;
    private readonly SemaphoreSlim writes = new(1, 1);
    private bool disposed;
    public event Action? SessionEnded;
    public AdminIdentity? Identity { get; private set; }
    public bool NeedsReconciliation { get; private set; }

    public ApiClient(ServerSettings settings, HttpMessageHandler? transport = null)
    {
        this.settings = settings;
        var address = ServerAddress.Validate(settings.BaseUrl);
        // The cookie jar is owned here even with an injected test transport; no disk persistence or automatic redirect/retry.
        var network = transport ?? new HttpClientHandler { UseCookies = false, AllowAutoRedirect = false };
        http = new HttpClient(new MemoryCookies(cookies, network)) { BaseAddress = address, Timeout = TimeSpan.FromSeconds(20) };
    }

    public async Task<ReadyInfo> ReadinessAsync(CancellationToken cancellationToken = default)
    {
        var ready = await ReadAsync<ReadyInfo>("/health/ready", false, cancellationToken);
        if (ready.Status != "ready" || string.IsNullOrWhiteSpace(ready.Environment) || string.IsNullOrWhiteSpace(ready.Fingerprint)) throw new AdminApiException("INVALID_RESPONSE");
        if ((!string.IsNullOrWhiteSpace(settings.ExpectedEnvironment) && settings.ExpectedEnvironment != ready.Environment) ||
            (!string.IsNullOrWhiteSpace(settings.ExpectedFingerprint) && settings.ExpectedFingerprint != ready.Fingerprint)) throw new AdminApiException("SERVER_MISMATCH");
        return ready;
    }

    public async Task<AdminIdentity> LoginAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        await writes.WaitAsync(cancellationToken);
        try
        {
            if (NeedsReconciliation) throw new AdminApiException("UNCERTAIN");
            if (Identity is not null) throw new AdminApiException("INVALID_STATE", 409);
            await ReadinessAsync(cancellationToken);
            using var response = await WriteAsync("/api/auth/login", new LoginInput(username, password), cancellationToken);
            AdminIdentity identity;
            try { identity = await Deserialize<AdminIdentity>(response, cancellationToken); }
            catch(AdminApiException){NeedsReconciliation=true;throw new AdminApiException("UNCERTAIN");}
            if (identity.Role != "ADMIN" || identity.Id <= 0 || string.IsNullOrWhiteSpace(identity.Username) || identity.ExpiresAt <= DateTimeOffset.UtcNow)
            { NeedsReconciliation=true; throw new AdminApiException("UNCERTAIN"); }
            Identity = identity; return identity;
        }
        catch (AdminApiException error) when (error.Code == "UNAUTHORIZED") { Reset(); throw new AdminApiException("LOGIN_FAILED", 401); }
        catch { if (Identity is null && !NeedsReconciliation) ClearCookies(); throw; }
        finally { writes.Release(); }
    }

    public async Task<AdminIdentity?> RecoverSessionAsync(CancellationToken cancellationToken = default)
    {
        await writes.WaitAsync(cancellationToken);
        try
        {
            await ReadinessAsync(cancellationToken);
            try
            {
                var identity = await ReadAsync<AdminIdentity>("/api/auth/session", false, cancellationToken);
                if(identity.Role != "ADMIN" || identity.Id <= 0 || identity.ExpiresAt <= DateTimeOffset.UtcNow)throw new AdminApiException("INVALID_RESPONSE");
                Identity = identity; NeedsReconciliation = false; return identity;
            }
            catch(AdminApiException error) when(error.Code == "UNAUTHORIZED"){Reset();return null;}
        }
        finally { writes.Release(); }
    }

    public async Task LogoutAsync(CancellationToken cancellationToken = default)
    {
        await writes.WaitAsync(cancellationToken);
        try
        {
            if (Identity is not null && !NeedsReconciliation)
            {
                try { using var response = await WriteAsync("/api/auth/logout", new EmptyInput(), cancellationToken); }
                catch (AdminApiException error) when (error.Code == "UNAUTHORIZED") { }
            }
        }
        finally { Reset(); writes.Release(); }
    }

    public async Task ChangePasswordAsync(string currentPassword, string newPassword, CancellationToken cancellationToken = default)
    {
        await CommandAsync("/api/account/password", new PasswordInput(currentPassword, newPassword), new CommandIntent(), cancellationToken);
        Reset();
    }

    public Task<T> GetAsync<T>(string path, CancellationToken cancellationToken = default) => ReadAsync<T>(path, true, cancellationToken);
    private async Task<T> ReadAsync<T>(string path, bool authenticated, CancellationToken cancellationToken)
    {
        ValidatePath(path); if (authenticated) RequireSession();
        try
        {
            using var response = await http.GetAsync(path, cancellationToken);
            await Check(response, cancellationToken); return await Deserialize<T>(response, cancellationToken);
        }
        catch (HttpRequestException) { throw new AdminApiException("NETWORK"); }
        catch (OperationCanceledException) { throw new AdminApiException("TIMEOUT"); }
    }

    public async Task CommandAsync<T>(string path, T input, CommandIntent intent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(intent); ValidatePath(path);
        await writes.WaitAsync(cancellationToken);
        try
        {
            RequireSession(); if (NeedsReconciliation) throw new AdminApiException("UNCERTAIN");
            using var response = await WriteAsync(path, input, cancellationToken);
        }
        finally { writes.Release(); }
    }

    public async Task ReconcileAsync(Func<CancellationToken, Task> readAuthoritativeResult, CancellationToken cancellationToken = default)
    {
        // Caller chooses a real GET/lookup/journal read; a failed read never clears the guard.
        await readAuthoritativeResult(cancellationToken); NeedsReconciliation = false;
    }

    public async Task<long> CreateAsync<T>(string path, T input, CancellationToken cancellationToken = default)
    {
        ValidatePath(path); await writes.WaitAsync(cancellationToken);
        try
        {
            RequireSession(); if (NeedsReconciliation) throw new AdminApiException("UNCERTAIN");
            using var response = await WriteAsync(path, input, cancellationToken);
            try
            {
                if (response.StatusCode != HttpStatusCode.Created) throw new AdminApiException("INVALID_RESPONSE");
                var created = await Deserialize<CreatedId>(response, cancellationToken);
                if (created.Id <= 0) throw new AdminApiException("INVALID_RESPONSE");
                return created.Id;
            }
            catch (AdminApiException) { NeedsReconciliation = true; throw new AdminApiException("UNCERTAIN"); }
        }
        finally { writes.Release(); }
    }

    public async Task PutAsync<T>(string path, T input, CancellationToken cancellationToken = default)
    {
        ValidatePath(path); await writes.WaitAsync(cancellationToken);
        try
        {
            RequireSession(); if (NeedsReconciliation) throw new AdminApiException("UNCERTAIN");
            using var response = await WriteAsync(path, input, cancellationToken, HttpMethod.Put);
            if (response.StatusCode != HttpStatusCode.NoContent) { NeedsReconciliation = true; throw new AdminApiException("UNCERTAIN"); }
        }
        finally { writes.Release(); }
    }
    private sealed record CreatedId(long Id);

    private async Task<HttpResponseMessage> WriteAsync<T>(string path, T input, CancellationToken cancellationToken, HttpMethod? method = null)
    {
        var token = await ReadAsync<TokenResult>("/api/security/token", false, cancellationToken);
        if (string.IsNullOrWhiteSpace(token.Token)) throw new AdminApiException("INVALID_RESPONSE");
        using var request = new HttpRequestMessage(method ?? HttpMethod.Post, path) { Content = JsonContent.Create(input) };
        request.Headers.Add("X-CSRF-TOKEN", token.Token);
        try
        {
            var response = await http.SendAsync(request, cancellationToken);
            try { await Check(response, cancellationToken); return response; }
            catch { response.Dispose(); throw; }
        }
        catch (HttpRequestException) { NeedsReconciliation = true; throw new AdminApiException("UNCERTAIN"); }
        catch (OperationCanceledException) { NeedsReconciliation = true; throw new AdminApiException("UNCERTAIN"); }
    }

    private async Task Check(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode) return;
        var status = (int)response.StatusCode; string? trace = null; string? code = null;
        try { var error = await response.Content.ReadFromJsonAsync<ErrorBody>(cancellationToken); code = error?.Code; trace = error?.TraceId; }
        catch (JsonException) { }
        if (status == 401) { Reset(); code = "UNAUTHORIZED"; }
        code ??= status switch { 400 => "INVALID_INPUT", 403 => "FORBIDDEN", 404 => "NOT_FOUND", 409 => "INVALID_STATE", 503 => "DATABASE_UNAVAILABLE", _ => "SERVER_ERROR" };
        throw new AdminApiException(code, status, trace);
    }
    private static async Task<T> Deserialize<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try { return await response.Content.ReadFromJsonAsync<T>(cancellationToken) ?? throw new AdminApiException("INVALID_RESPONSE"); }
        catch (JsonException) { throw new AdminApiException("INVALID_RESPONSE"); }
        catch (NotSupportedException) { throw new AdminApiException("INVALID_RESPONSE"); }
    }
    private static void ValidatePath(string path)
    {
        if (!path.StartsWith('/') || path.StartsWith("//", StringComparison.Ordinal) || path.Contains('\\') || path.Contains("..", StringComparison.Ordinal)) throw new AdminApiException("INVALID_INPUT");
    }
    private void RequireSession()
    {
        if (Identity is null) throw new AdminApiException("UNAUTHORIZED", 401);
        if (Identity.ExpiresAt <= DateTimeOffset.UtcNow) { Reset(); throw new AdminApiException("UNAUTHORIZED", 401); }
    }
    private void ClearCookies() { foreach (Cookie cookie in cookies.GetAllCookies()) cookie.Expired = true; }
    private void Reset() { var wasAuthenticated = Identity is not null; Identity = null; NeedsReconciliation = false; ClearCookies(); if (wasAuthenticated) SessionEnded?.Invoke(); }
    public void Dispose() { if (disposed) return; disposed = true; Identity = null; ClearCookies(); http.Dispose(); writes.Dispose(); }
    private sealed record ErrorBody(string? Code, string? TraceId);
    private sealed class MemoryCookies(CookieContainer jar, HttpMessageHandler inner) : DelegatingHandler(inner)
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var uri = request.RequestUri!; var cookie = jar.GetCookieHeader(uri); if (cookie.Length > 0) request.Headers.TryAddWithoutValidation("Cookie", cookie);
            var response = await base.SendAsync(request, cancellationToken);
            if (response.Headers.TryGetValues("Set-Cookie", out var values)) foreach (var value in values) jar.SetCookies(uri, value);
            return response;
        }
    }
}
