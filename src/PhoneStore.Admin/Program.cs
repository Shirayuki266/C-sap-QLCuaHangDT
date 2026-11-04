using PhoneStore.Admin.Client;

namespace PhoneStore.Admin;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize(); Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, _) => MessageBox.Show("Ứng dụng gặp sự cố. Vui lòng đóng màn hình và đăng nhập lại.", "Monophone Admin", MessageBoxButtons.OK, MessageBoxIcon.Error);
        AppDomain.CurrentDomain.UnhandledException += (_, _) => MessageBox.Show("Ứng dụng phải đóng. Vui lòng mở lại và kiểm tra dữ liệu trước khi gửi tiếp.", "Monophone Admin");
        using var context = new AdminApplication(); Application.Run(context);
    }
}

public sealed class AdminApplication : ApplicationContext
{
    private readonly SettingsStore store;
    private readonly Func<ServerSettings, IAdminApi> factory;
    private IAdminApi? api;
    private LoginForm? login;
    private MainForm? shell;
    private bool disposing;
    public AdminApplication(Func<ServerSettings, IAdminApi>? factory = null, SettingsStore? store = null)
    { this.factory = factory ?? (settings => new ApiClient(settings)); this.store = store ?? new(); ShowLogin(); }
    private void ShowLogin()
    {
        if (disposing) return;
        var settings = store.Load(); api?.Dispose(); api = factory(settings);
        var sessionApi = api;
        api.SessionEnded += () =>
        {
            if (!ReferenceEquals(api, sessionApi) || shell is not { IsDisposed: false } current) return;
            current.BeginInvoke(() => { if (ReferenceEquals(shell, current) && ReferenceEquals(api, sessionApi)) ReturnToLogin(); });
        };
        login = new LoginForm(api, settings); MainForm = login;
        login.ConfigurationChanged += changed =>
        {
            try { store.Save(changed); MainForm = null; login.Close(); ShowLogin(); }
            catch (Exception) { MessageBox.Show("Chưa lưu được cấu hình. Vui lòng kiểm tra quyền thư mục người dùng."); }
        };
        login.Authenticated += () =>
        {
            shell = new MainForm(api, login.Ready!);
            shell.LoggedOut += ReturnToLogin;
            MainForm = shell; shell.Show(); login.Hide(); login.Dispose(); login = null;
        };
        login.Show();
    }
    private void ReturnToLogin()
    {
        if (disposing || shell is null) return;
        var oldShell = shell; oldShell.Navigator.Dispose(); if(HasPurchaseWindow(oldShell))MessageBox.Show(oldShell,"Phiên quản trị đã kết thúc. Cửa sổ phiếu nhập sẽ đóng; nội dung chưa lưu không được giữ. Hãy đăng nhập lại và nhập lại nháp. Ứng dụng không tự gửi lại lệnh ghi bằng phiên cũ.","Nháp chưa lưu",MessageBoxButtons.OK,MessageBoxIcon.Warning); var exit = oldShell.ClosingApplication; shell = null; MainForm = null; oldShell.EndSession();
        if (exit) { disposing = true; api?.Dispose(); ExitThread(); } else ShowLogin();
    }
    private static bool HasPurchaseWindow(Form owner)=>owner.OwnedForms.Any(f=>f is PurchaseEditor||HasPurchaseWindow(f));
    protected override void Dispose(bool disposing)
    { this.disposing = true; if (disposing) { api?.Dispose(); login?.Dispose(); shell?.Dispose(); } base.Dispose(disposing); }
}
