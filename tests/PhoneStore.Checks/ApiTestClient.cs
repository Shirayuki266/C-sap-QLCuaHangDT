using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace PhoneStore.Checks;

internal sealed class ApiTestClient : IDisposable
{
    public CookieContainer Cookies { get; } = new();
    public HttpClient Http { get; }

    public ApiTestClient(Uri address)
    {
        Http = new(new HttpClientHandler { UseProxy = false, AllowAutoRedirect = false, CookieContainer = Cookies })
        { BaseAddress = address, Timeout = TimeSpan.FromSeconds(15) };
    }

    public async Task<string> Token()
    {
        using var response = await Http.GetAsync("/api/security/token");
        Check.True(response.IsSuccessStatusCode, "Could not obtain CSRF token.");
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.GetProperty("token").GetString()!;
    }

    public async Task<HttpResponseMessage> Send(string route, object body, HttpMethod? method = null, bool csrf = true)
    {
        using var request = new HttpRequestMessage(method ?? HttpMethod.Post, route) { Content = JsonContent.Create(body) };
        if (csrf) request.Headers.Add("X-CSRF-TOKEN", await Token());
        return await Http.SendAsync(request);
    }

    public string SessionCookie() => Cookies.GetCookies(Http.BaseAddress!).Cast<Cookie>().Single(x => x.Name == "PhoneStore.Session").Value;
    public void Replay(string cookie) => Cookies.Add(Http.BaseAddress!, new Cookie("PhoneStore.Session", cookie, "/"));
    public void Dispose() => Http.Dispose();
}
