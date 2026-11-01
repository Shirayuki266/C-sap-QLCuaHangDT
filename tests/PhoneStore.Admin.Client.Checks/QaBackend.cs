using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PhoneStore.Infrastructure.Data;
using PhoneStore.Web.Storefront;

namespace PhoneStore.Admin.Client.Checks;

internal sealed class QaBackend : WebApplicationFactory<StorefrontEntryPoint>, IAsyncDisposable
{
    public string Connection { get; private set; } = "";
    private string master = "", name = "";
    public string Password { get; } = "QaOnly@" + Guid.NewGuid().ToString("N");
    public string NextPassword { get; } = "QaNext@" + Guid.NewGuid().ToString("N");
    public long AdminId { get; private set; }
    public readonly List<string> LogMessages = [];
    public static string Root
    {
        get { var dir = new DirectoryInfo(AppContext.BaseDirectory); while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "PhoneStore.slnx"))) dir = dir.Parent; return dir?.FullName ?? throw new InvalidOperationException("Root missing"); }
    }
    public static async Task<QaBackend> Create()
    {
        var configured = SqlTarget.Validate(Environment.GetEnvironmentVariable("PHONESTORE_TEST_CONNECTION"));
        var target = new SqlConnectionStringBuilder(configured);
        if (!target.InitialCatalog.StartsWith("PhoneStore_QA_", StringComparison.Ordinal) || target.DataSource != Environment.GetEnvironmentVariable("PHONESTORE_QA_SERVER") || target.InitialCatalog != Environment.GetEnvironmentVariable("PHONESTORE_QA_DATABASE")) throw new InvalidOperationException("Explicit matching QA target required.");
        var fixture = new QaBackend { name = "PhoneStore_QA_Admin_" + Guid.NewGuid().ToString("N") };
        target.Pooling = false; target.InitialCatalog = "master"; fixture.master = target.ConnectionString;
        target.InitialCatalog = fixture.name; fixture.Connection = target.ConnectionString;
        await using (var connection = new SqlConnection(fixture.master)) { await connection.OpenAsync(); await using var command = connection.CreateCommand(); command.CommandText = $"CREATE DATABASE [{fixture.name}]"; await command.ExecuteNonQueryAsync(); }
        await using var db = fixture.Open(); await new DatabaseMigrator(db).MigrateAsync();
        var hasher = new PasswordHasher<Account>();
        foreach (var (username, role, active) in new[] { ("admin-w1-qa", "ADMIN", true), ("customer-w1-qa", "CUSTOMER", true), ("locked-w1-qa", "ADMIN", false) })
        {
            var account = new Account { Username = username, Role = role, Active = active }; account.PasswordHash = hasher.HashPassword(account, fixture.Password); db.Accounts.Add(account); await db.SaveChangesAsync();
            if (role == "CUSTOMER") db.Customers.Add(new Customer { Id = account.Id, FullName = "Khách QA", Phone = "0938123591", Address = "QA only" });
            else if (active) fixture.AdminId = account.Id;
        }
        await db.SaveChangesAsync(); return fixture;
    }
    public StoreDbContext Open() => new(new DbContextOptionsBuilder<StoreDbContext>().UseSqlServer(Connection).Options);
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing").UseContentRoot(Path.Combine(Root, "src", "PhoneStore.Web")).UseSetting("ConnectionStrings:Store", Connection);
        builder.ConfigureServices(services => { services.AddHttpContextAccessor(); services.AddDataProtection().UseEphemeralDataProtectionProvider(); });
        builder.ConfigureLogging(logging => { logging.ClearProviders(); logging.AddProvider(new MemoryLog(LogMessages)); });
    }
    public ApiClient Api(DelegatingHandler? probe = null, ServerSettings? settings = null)
    {
        HttpMessageHandler transport = Server.CreateHandler(); if (probe is not null) { probe.InnerHandler = transport; transport = probe; }
        return new(settings ?? new("https://localhost"), transport);
    }
    public new async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        if (name.Length == 0) return;
        await using var connection = new SqlConnection(master); await connection.OpenAsync(); await using var cleanup = connection.CreateCommand();
        cleanup.CommandText = $"DROP DATABASE [{name}]"; await cleanup.ExecuteNonQueryAsync();
    }
    private sealed class MemoryLog(List<string> messages) : ILoggerProvider
    {
        public ILogger CreateLogger(string categoryName) => new Sink(messages);
        public void Dispose() { }
        private sealed class Sink(List<string> messages) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
            public bool IsEnabled(LogLevel logLevel) => true;
            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { lock (messages) messages.Add(formatter(state, exception)); }
        }
    }
}
