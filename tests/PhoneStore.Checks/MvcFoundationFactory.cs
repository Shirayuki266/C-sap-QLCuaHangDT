using System.Data.Common;
using System.Net;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using PhoneStore.Core;
using PhoneStore.Infrastructure.Data;
using PhoneStore.Web.Security;
using PhoneStore.Web.Storefront;

namespace PhoneStore.Checks;

internal sealed class MvcFoundationFactory(string connection, string root, string environment = "Testing") : WebApplicationFactory<StorefrontEntryPoint>
{
    public CartCommandProbe CartProbe { get; } = new();
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(environment);
        builder.UseContentRoot(Path.Combine(root, "src", "PhoneStore.Web"));
        builder.UseSetting("ConnectionStrings:Store", connection);
        builder.ConfigureServices(services =>
        {
            services.AddDataProtection().UseEphemeralDataProtectionProvider();
            services.AddDbContext<StoreDbContext>(options => options.AddInterceptors(CartProbe));
            services.AddControllersWithViews().AddApplicationPart(typeof(MvcFoundationProbeController).Assembly);
            services.Configure<StorefrontOptions>(options => options.ContactNote = "<script>private-html-probe</script>");
        });
    }
    public HttpClient Browser() => CreateClient(new WebApplicationFactoryClientOptions
        { AllowAutoRedirect = false, BaseAddress = new Uri("https://localhost") });
}

internal sealed class CartCommandProbe : DbCommandInterceptor
{
    public bool Enabled { get; set; }
    public int CartCommands { get; private set; }
    private void Observe(DbCommand command)
    {
        if (Enabled && command.CommandText.Contains("GIO_HANG_CT", StringComparison.OrdinalIgnoreCase)) CartCommands++;
    }
    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData,
        InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
    { Observe(command); return ValueTask.FromResult(result); }
    public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command, CommandEventData eventData,
        InterceptionResult<int> result, CancellationToken cancellationToken = default)
    { Observe(command); return ValueTask.FromResult(result); }
}

// Added as an application part only by this test factory, never by the shipping host.
[ApiExplorerSettings(IgnoreApi = true)]
public sealed class MvcFoundationProbeController : Controller
{
    [HttpGet("/__foundation/token")]
    public IActionResult Token([FromServices] IAntiforgery antiforgery) => Json(new { token = antiforgery.GetAndStoreTokens(HttpContext).RequestToken });

    [Authorize(Policy = "Customer"), HttpGet("/Cart/__foundation")]
    public IActionResult Private() => Content("customer-only");

    [Authorize(Policy = "Customer"), HttpPost("/__foundation/selection")]
    public async Task<IActionResult> Select([FromServices] IStoreService store, long productId, long variantId, int quantity)
    {
        var current = SessionAuthentication.Current(HttpContext);
        await store.AddToCartAsync(new(current.Account.Id, current.SessionId), productId, variantId, quantity);
        return NoContent();
    }

    [HttpPost("/__foundation/json")]
    public IActionResult JsonPost([FromBody] Dictionary<string, int> input) => Json(input);

    [HttpGet("/__foundation/error")]
    public IActionResult Error() => throw new InvalidOperationException("Do not expose private-failure-details");

    [HttpGet("/__foundation/business/{code}")]
    public IActionResult Business(string code) => throw new BusinessException(code, "Service message");
}
