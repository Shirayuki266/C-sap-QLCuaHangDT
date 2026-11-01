using System.Text.Json;
using PhoneStore.Admin.Client;

namespace PhoneStore.Admin.Tests;

internal static partial class Program
{
    private static int passed;
    private static string output = "";
    private static readonly List<object> captures = [];
    [STAThread]
    private static int Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        output = Environment.GetEnvironmentVariable("PHONESTORE_ADMIN_UI_EVIDENCE") ?? Path.Combine(FindRoot(), ".artifacts", "admin-w1", "forms"); Directory.CreateDirectory(output);
        using var host = new Form { ShowInTaskbar = false, Size = new Size(1, 1), Opacity = 0 };
        var failed = false;
        host.Shown += async (_, _) =>
        {
            try { await Run(args); Console.WriteLine($"{passed} admin form checks passed."); }
            catch (Exception error) { failed = true; Console.Error.WriteLine("FAIL form check: " + error.Message); }
            finally { host.Close(); }
        };
        Application.Run(host); return failed ? 1 : 0;
    }
    private static void Assert(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static async Task Test(string name, Func<Task> action) { await action(); Console.WriteLine("PASS " + name); passed++; }
    private static async Task Capture(Form form, string name)
    {
        await Task.Delay(70); form.PerformLayout(); form.Refresh();
        using var bitmap = new Bitmap(form.Width, form.Height); form.DrawToBitmap(bitmap, new Rectangle(Point.Empty, form.Size)); bitmap.Save(Path.Combine(output, name + ".png"));
        captures.Add(new { name, width=form.Width, height=form.Height, dpi=form.DeviceDpi, minimumWidth=form.MinimumSize.Width, minimumHeight=form.MinimumSize.Height });
    }
    private static string FindRoot() { var root = new DirectoryInfo(AppContext.BaseDirectory); while (root is not null && !File.Exists(Path.Combine(root.FullName, "PhoneStore.slnx"))) root = root.Parent; return root?.FullName ?? throw new InvalidOperationException("Root missing"); }
    private static async Task Run(string[] args)
    {
        if (args.Contains("--real-api")) { await RealApi(); return; }
        if (args.Contains("--real-catalog")) { await RealCatalog(); return; }
        if (args.Contains("--real-contacts")) { await RealContacts(); return; }
        var fake = new FakeApi(); var settings = new ServerSettings("https://qa.example.test");
        await Test("WIN.BASE.02 Windows restored application assets exclude backend/EF/SQL packages", () =>
        {
            foreach(var project in new[]{"PhoneStore.Admin","PhoneStore.Admin.Client"})
            {
                var assets=File.ReadAllText(Path.Combine(FindRoot(),"src",project,"obj","project.assets.json"));
                foreach(var forbidden in new[]{"PhoneStore.Infrastructure","PhoneStore.Web","Microsoft.EntityFrameworkCore","Microsoft.Data.SqlClient","System.Data.SqlClient"})Assert(!assets.Contains(forbidden),"Forbidden Windows application dependency");
            }
            return Task.CompletedTask;
        });
        await Test("WIN.A01/BASE.14 actual STA LoginForm clears password before await, guards duplicate, renders ready/busy/error", async () =>
        {
            using var form = new LoginForm(fake, settings); form.Show(); await Task.Delay(70);
            Assert(form.ServerLabel.Text.Contains("QA"), "Ready metadata not shown"); Assert(form.AcceptButton == form.SubmitButton && form.AutoScaleMode == AutoScaleMode.Dpi, "Keyboard/DPI configuration missing");
            await Capture(form, "UI01-login-1280");
            form.UsernameBox.Text = "qa-admin"; form.PasswordBox.Text = Guid.NewGuid().ToString("N"); fake.HoldLogin = new(TaskCreationOptions.RunContinuationsAsynchronously);
            form.PasswordBox.Focus();
            typeof(Form).GetMethod("ProcessDialogKey",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!.Invoke(form,[Keys.Enter]);
            form.SubmitButton.PerformClick(); await Task.Delay(40);
            Assert(fake.LoginCalls == 1 && form.PasswordBox.Text.Length == 0 && !form.SubmitButton.Enabled && form.SubmitButton.Text.Contains("Đang") && form.ProgressLabel.Visible && form.ProgressLabel.Text.Contains("Đang"), "Submit guard/secret clearing/readable status failed"); await Capture(form, "UI01-login-busy");
            fake.HoldLogin.SetException(new AdminApiException("LOGIN_FAILED", 401)); await Task.Delay(70);
            Assert(form.SubmitButton.Enabled && form.UsernameBox.Text == "qa-admin" && form.PasswordBox.Text.Length == 0 && form.MessageLabel.Text.Contains("Tên đăng nhập"), "Failed login retention/recovery failed"); await Capture(form, "UI01-login-error");
            fake.ReadyError = new AdminApiException("NETWORK"); await form.CheckAsync(); Assert(form.MessageLabel.Text.Contains("Không kết nối"), "Network recovery missing"); await Capture(form, "UI01-offline"); fake.ReadyError = null; form.Close();
        });
        await Test("WIN.BASE.13 shell navigation placeholders no fake commerce; account controls and QA status", async () =>
        {
            fake.Identity = new(1, "qa-admin", "ADMIN", DateTimeOffset.UtcNow.AddHours(8)); using var form = new MainForm(fake, FakeApi.Ready); form.Show(); await Task.Delay(40);
            Assert(form.Navigation.Count == 11 && form.MinimumSize.Width >= 1280 && form.MinimumSize.Height >= 800 && form.Width >= 1280 && form.Height >= 800,
                $"Shell structure missing: navigation={form.Navigation.Count};minimum={form.MinimumSize};size={form.Size};dpi={form.DeviceDpi}");
            foreach (var button in form.Navigation.Where(b=>b.Text is not ("Điện thoại" or "Hãng" or "Danh mục" or "Khách hàng" or "Nhà cung cấp"))) { button.PerformClick(); Assert(form.TitleLabel.Text == button.Text, "Navigation not operational"); if (button.Text != "Tài khoản") Assert(form.ContentLabel.Text == "Chưa triển khai", "Placeholder invents delivered capability"); }
            form.Navigation[0].PerformClick(); Assert(form.StatusLabel.Text.Contains("QA") && form.StatusLabel.Text.Contains("qa-admin"), "Session/server status absent"); await Capture(form, "UI02-main-1280");
            form.EndSession();
        });
        await Test("WIN.A03 form submits only self passwords, clears fields and retains controlled errors", async () =>
        {
            using var form = new PasswordForm(fake); form.Show(); fake.PasswordError = new AdminApiException("INVALID_INPUT", 400);
            form.CurrentBox.Text = "wrong-qa"; form.NewBox.Text = "short"; form.SaveButton.PerformClick(); await Task.Delay(50);
            Assert(form.CurrentBox.Text.Length == 0 && form.NewBox.Text.Length == 0 && form.MessageLabel.Text.Contains("Dữ liệu"), "Password fields or safe error wrong"); await Capture(form, "password-error");
            fake.PasswordError = null; form.CurrentBox.Text = "current-qa"; form.NewBox.Text = "new-qa"; await form.SubmitAsync(); Assert(form.DialogResult == DialogResult.OK, "Password success failed"); form.Close();
        });
        await Test("WIN.BASE.18 user settings only allow public server configuration, validation preserves form", async () =>
        {
            using var form = new ServerSettingsForm(settings); form.Show(); form.AddressBox.Text = "http://remote.example"; form.SaveButton.PerformClick();
            Assert(form.Result is null && form.MessageLabel.Text.Contains("HTTPS") && form.AddressBox.Text == "http://remote.example", "Unsafe config accepted/lost input"); await Capture(form, "settings-invalid");
            form.AddressBox.Text = "http://localhost:5080"; form.EnvironmentBox.Text = "Testing"; form.FingerprintBox.Text = "qa-expected"; form.SaveButton.PerformClick(); Assert(form.Result is not null, "Valid configuration rejected");
            var directory = Path.Combine(output, "settings-qa"); var store = new SettingsStore(directory); store.Save(form.Result!); Assert(store.Load() == form.Result, "Settings roundtrip failed");
            var text = File.ReadAllText(Path.Combine(directory, "settings.json")); Assert(!text.Contains("Password", StringComparison.OrdinalIgnoreCase) && !text.Contains("cookie", StringComparison.OrdinalIgnoreCase) && !text.Contains("token", StringComparison.OrdinalIgnoreCase), "Secret settings property"); form.Close();
        });
        await Test("WIN.A02 bounded best-effort close and logout work without a live server", async () =>
        {
            fake.Identity = new(1, "qa-admin", "ADMIN", DateTimeOffset.UtcNow.AddHours(8)); using var form = new MainForm(fake, FakeApi.Ready); form.Show();
            fake.HangLogout = true; var watch = System.Diagnostics.Stopwatch.StartNew(); form.Close(); await Task.Delay(2200);
            Assert(form.IsDisposed && fake.LogoutCalls == 1 && watch.Elapsed < TimeSpan.FromSeconds(4), "Best-effort close hung or duplicated logout"); fake.HangLogout = false;
        });
        await Test("WIN.BASE.07 production application context returns to LoginForm on revoked session", async () =>
        {
            using var application = new AdminApplication(_ => fake, new SettingsStore(Path.Combine(output, "context-settings-qa")));
            await Task.Delay(50); var login = (LoginForm)application.MainForm!; fake.HoldLogin = null;
            login.UsernameBox.Text = "qa-admin"; login.PasswordBox.Text = "qa-test-input"; await login.SubmitAsync(); await Task.Delay(50);
            Assert(application.MainForm is MainForm, "Actual application did not switch to MainForm");
            fake.Expire(); await Task.Delay(80); Assert(application.MainForm is LoginForm && fake.Identity is null, "Actual application failed session-expiry redirect");
        });
        await Test("WIN.BASE.15 reusable confirmation dialog defaults to safe dismissal", async () =>
        {
            using var dialog = new ConfirmationDialog("Xác nhận thao tác", "Kiểm tra dữ liệu trước khi gửi. Chọn Quay lại để giữ nguyên."); dialog.Show();
            Assert(dialog.ActiveControl == dialog.DismissButton && dialog.CancelButton == dialog.DismissButton, "Confirmation lacks safe dismissal");
            await Capture(dialog, "confirmation-foundation"); dialog.DismissButton.PerformClick(); Assert(dialog.DialogResult == DialogResult.Cancel, "Dismiss did not cancel"); dialog.Close();
        });
        File.WriteAllText(Path.Combine(output, "results.json"), JsonSerializer.Serialize(new { platform = "Windows STA WinForms", fakeApi = true, checks = passed, logicalMinimum = "1280x800 for main/login; physical dimensions depend on DPI", captures, notes = "Actual form controls/PerformClick/DrawToBitmap; actual backend/SQL proofs live in Admin.Client.Checks. No fake evidence is labeled as real API." }, new JsonSerializerOptions { WriteIndented = true }));
        await CatalogFormsChecks();
        await ContactFormsChecks();
    }
    private static async Task RealApi()
    {
        var address = Environment.GetEnvironmentVariable("PHONESTORE_ADMIN_QA_URL") ?? throw new InvalidOperationException("Explicit loopback QA URL required");
        if (!ServerAddress.Validate(address).IsLoopback) throw new InvalidOperationException("Real UI QA must be loopback");
        var username = Environment.GetEnvironmentVariable("PHONESTORE_ADMIN_QA_USERNAME") ?? throw new InvalidOperationException("QA identity missing");
        var password = Environment.GetEnvironmentVariable("PHONESTORE_ADMIN_QA_PASSWORD") ?? throw new InvalidOperationException("QA credential missing");
        using var api = new ApiClient(new(address, "Testing")); using var login = new LoginForm(api, new(address)); login.Show(); await Task.Delay(80);
        for(var attempt=0;attempt<100&&!login.CheckButton.Enabled;attempt++)await Task.Delay(50);
        Assert(login.ServerLabel.Text.Contains("Testing"), "Real readiness not shown"); await Capture(login, "UI01-real-qa-login");
        login.UsernameBox.Text = username; login.PasswordBox.Text = password; password = ""; await login.SubmitAsync(); Assert(api.Identity?.Role == "ADMIN", "Real WinForms login failed");
        using var main = new MainForm(api, await api.ReadinessAsync()); main.Show(); await Capture(main, "UI02-real-qa-main"); await main.LogoutAsync(); Assert(api.Identity is null, "Real UI logout left identity"); login.Close();
        passed++; Console.WriteLine("PASS actual WinForms controls -> real loopback Kestrel -> SQL QA login/logout; captured both screens");
        File.WriteAllText(Path.Combine(output,"results.json"),JsonSerializer.Serialize(new{fakeApi=false,qaOnly=true,captures},new JsonSerializerOptions{WriteIndented=true}));
    }
}

internal sealed class FakeApi : IAdminApi
{
    public static ReadyInfo Ready => new("ready", "QA · API giả", "fixture-only-no-server");
    public AdminIdentity? Identity { get; set; }
    public bool NeedsReconciliation => false;
    public event Action? SessionEnded;
    public int LoginCalls, LogoutCalls;
    public TaskCompletionSource<AdminIdentity>? HoldLogin;
    public Exception? ReadyError, PasswordError;
    public bool HangLogout;
    public void Expire() { Identity = null; SessionEnded?.Invoke(); }
    public Task<ReadyInfo> ReadinessAsync(CancellationToken cancellationToken = default) => ReadyError is null ? Task.FromResult(Ready) : Task.FromException<ReadyInfo>(ReadyError);
    public Task<AdminIdentity?> RecoverSessionAsync(CancellationToken cancellationToken = default) => Task.FromResult(Identity);
    public async Task<AdminIdentity> LoginAsync(string username, string password, CancellationToken cancellationToken = default) { LoginCalls++; var result = HoldLogin is null ? new AdminIdentity(1, username, "ADMIN", DateTimeOffset.UtcNow.AddHours(8)) : await HoldLogin.Task; Identity = result; return result; }
    public async Task LogoutAsync(CancellationToken cancellationToken = default) { LogoutCalls++; try { if (HangLogout) await Task.Delay(Timeout.Infinite, cancellationToken); } finally { Identity = null; SessionEnded?.Invoke(); } }
    public Task ChangePasswordAsync(string currentPassword, string newPassword, CancellationToken cancellationToken = default) { if (PasswordError is not null) return Task.FromException(PasswordError); Identity = null; SessionEnded?.Invoke(); return Task.CompletedTask; }
    public Task<T> GetAsync<T>(string path, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task CommandAsync<T>(string path, T input, CommandIntent intent, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public async Task ReconcileAsync(Func<CancellationToken, Task> readAuthoritativeResult, CancellationToken cancellationToken = default) => await readAuthoritativeResult(cancellationToken);
    public void Dispose() { }
}
