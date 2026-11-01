using PhoneStore.Admin.Client;

namespace PhoneStore.Admin;

public sealed class LoginForm : Form
{
    private readonly IAdminApi api;
    private readonly CancellationTokenSource lifetime = new();
    private bool busy;
    public event Action? Authenticated;
    public event Action<ServerSettings>? ConfigurationChanged;
    public ReadyInfo? Ready { get; private set; }
    public TextBox UsernameBox { get; } = AdminTheme.Input("Tên đăng nhập");
    public TextBox PasswordBox { get; } = AdminTheme.Input("Mật khẩu", true);
    public Button SubmitButton { get; } = AdminTheme.Button("&Đăng nhập");
    public Button CheckButton { get; } = AdminTheme.Button("&Kiểm tra kết nối", false);
    public Button ConfigureButton { get; } = AdminTheme.Button("&Cấu hình máy chủ", false);
    public Button ExitButton { get; } = AdminTheme.Button("Đó&ng", false);
    public Label ServerLabel { get; } = AdminTheme.Label("Chưa kiểm tra kết nối.");
    public Label MessageLabel { get; } = AdminTheme.Label("");
    public Label ProgressLabel { get; } = AdminTheme.Label("", 10, true);
    public LoginForm(IAdminApi api, ServerSettings settings)
    {
        this.api = api; AdminTheme.Frame(this, "Đăng nhập quản trị");
        var frame = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, ColumnCount = 1 };
        frame.RowStyles.Add(new RowStyle(SizeType.Absolute, 58)); frame.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); frame.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        var header = AdminTheme.Label("Monophone Admin  ·  Quản lý cửa hàng điện thoại", 11, true); header.BackColor = Color.White; header.Dock = DockStyle.Fill; header.Padding = new Padding(24, 10, 0, 0); frame.Controls.Add(header, 0, 0);
        var center = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 3 };
        center.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25)); center.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50)); center.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        center.RowStyles.Add(new RowStyle(SizeType.Percent, 50)); center.RowStyles.Add(new RowStyle(SizeType.AutoSize)); center.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        var card = AdminTheme.Stack(32); card.Dock = DockStyle.Top; card.AutoSize = true;
        var brand = AdminTheme.Label("MONOPHONE · QUẢN TRỊ", 11, true); brand.ForeColor = AdminTheme.Blue; AdminTheme.Add(card, brand);
        AdminTheme.Add(card, AdminTheme.Label("Đăng nhập quản trị", 24, true)); AdminTheme.Add(card, AdminTheme.Label("Tiếp tục với tài khoản của bạn."));
        var endpoint = AdminTheme.Label(settings.BaseUrl); endpoint.MaximumSize = new Size(550, 0); AdminTheme.Add(card, endpoint);
        ServerLabel.MaximumSize = new Size(550, 0); ServerLabel.ForeColor = AdminTheme.Muted; AdminTheme.Add(card, ServerLabel);
        AdminTheme.Field(card, "&Tên đăng nhập *", UsernameBox); AdminTheme.Field(card, "&Mật khẩu *", PasswordBox);
        ProgressLabel.Visible = false; ProgressLabel.AccessibleRole = AccessibleRole.StatusBar; AdminTheme.Add(card, ProgressLabel);
        MessageLabel.MaximumSize = new Size(550, 0); MessageLabel.ForeColor = Color.FromArgb(170, 30, 45); MessageLabel.AccessibleRole = AccessibleRole.Alert; AdminTheme.Add(card, MessageLabel);
        SubmitButton.Dock = DockStyle.Fill; AdminTheme.Add(card, SubmitButton);
        var tools = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill }; tools.Controls.Add(CheckButton); tools.Controls.Add(ConfigureButton); tools.Controls.Add(ExitButton); AdminTheme.Add(card, tools);
        center.Controls.Add(card, 1, 1); frame.Controls.Add(center, 0, 1);
        var footer = AdminTheme.Label("Kết nối an toàn · Phiên tối đa 8 giờ · Không lưu mật khẩu", 9); footer.Padding = new Padding(24, 0, 0, 0); frame.Controls.Add(footer, 0, 2); Controls.Add(frame);
        AcceptButton = SubmitButton; CancelButton = ExitButton; ExitButton.Click += (_, _) => Close();
        SubmitButton.Click += async (_, _) => await SubmitAsync(); CheckButton.Click += async (_, _) => await CheckAsync();
        ConfigureButton.Click += (_, _) => { using var dialog = new ServerSettingsForm(settings); if (dialog.ShowDialog(this) == DialogResult.OK) ConfigurationChanged?.Invoke(dialog.Result!); };
        Shown += async (_, _) => { UsernameBox.Focus(); await CheckAsync(); };
        FormClosed += (_, _) => { lifetime.Cancel(); PasswordBox.Clear(); lifetime.Dispose(); };
    }
    public async Task CheckAsync()
    {
        if (busy) return; SetBusy(true, "Đang kiểm tra…");
        try
        {
            var ready = await api.ReadinessAsync(lifetime.Token); Ready = ready; ServerLabel.Text = $"{ready.Environment} · {ready.Fingerprint}"; MessageLabel.Text = "";
            if(api.NeedsReconciliation)
            {
                var recovered = await api.RecoverSessionAsync(lifetime.Token);
                if(recovered is not null)Authenticated?.Invoke();else MessageLabel.Text = "Đã kiểm tra phiên. Bạn có thể đăng nhập lại.";
            }
        }
        catch (Exception error) { if (!IsDisposed) { ServerLabel.Text = "Kết nối chưa sẵn sàng"; MessageLabel.Text = AdminTheme.Failure(error); } }
        finally { if (!IsDisposed) SetBusy(false); }
    }
    public async Task SubmitAsync()
    {
        if (busy) return;
        if (string.IsNullOrWhiteSpace(UsernameBox.Text) || PasswordBox.Text.Length == 0) { MessageLabel.Text = "Vui lòng nhập tên đăng nhập và mật khẩu."; PasswordBox.Clear(); return; }
        var password = PasswordBox.Text; PasswordBox.Clear(); SetBusy(true, "Đang đăng nhập…"); MessageLabel.Text = "";
        try { Ready = await api.ReadinessAsync(lifetime.Token); await api.LoginAsync(UsernameBox.Text.Trim(), password, lifetime.Token); if (!IsDisposed) Authenticated?.Invoke(); }
        catch (Exception error) { if (!IsDisposed) MessageLabel.Text = AdminTheme.Failure(error); }
        finally { password = ""; if (!IsDisposed) { PasswordBox.Clear(); SetBusy(false); } }
    }
    private void SetBusy(bool value, string text = "Đăng nhập") { busy = value; SubmitButton.Enabled = CheckButton.Enabled = ConfigureButton.Enabled = !value; SubmitButton.Text = value ? text : "&Đăng nhập"; ProgressLabel.Text = text; ProgressLabel.Visible = value; UseWaitCursor = value; }
}
