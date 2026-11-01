using PhoneStore.Admin.Client;

namespace PhoneStore.Admin;

public sealed class MainForm : Form
{
    private readonly IAdminApi api;
    private bool ending;
    public bool ClosingApplication { get; private set; }
    public event Action? LoggedOut;
    public Label TitleLabel { get; } = AdminTheme.Label("Tổng quan", 23, true);
    public Label ContentLabel { get; } = AdminTheme.Label("Chưa triển khai", 18, true);
    public Label StatusLabel { get; } = AdminTheme.Label("");
    public Button LogoutButton { get; } = AdminTheme.Button("Đăng &xuất", false);
    public Button PasswordButton { get; } = AdminTheme.Button("Đổi &mật khẩu", false);
    public IReadOnlyList<Button> Navigation => navigation;
    private readonly List<Button> navigation = [];
    public MainForm(IAdminApi api, ReadyInfo ready)
    {
        this.api = api; AdminTheme.Frame(this, "Quản trị cửa hàng");
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2 };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 230)); root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); root.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        var side = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true, BackColor = AdminTheme.Navy, Padding = new Padding(16) };
        var brand = AdminTheme.Label("Monophone.", 18, true); brand.AutoSize = false; brand.Size = new Size(190, 54); brand.ForeColor = Color.White; side.Controls.Add(brand);
        foreach (var name in new[] { "Tổng quan", "Điện thoại", "Khách hàng", "Nhập hàng", "Tồn kho", "Đơn hàng", "Báo cáo", "Hãng", "Danh mục", "Nhà cung cấp", "Tài khoản" })
        {
            var button = AdminTheme.Button(name, false); button.Width = 190; button.Height = 40; navigation.Add(button); side.Controls.Add(button);
            button.Click += (_, _) =>
            {
                TitleLabel.Text = name; ContentLabel.Text = name == "Tài khoản" ? "Quản lý phiên và mật khẩu của bạn" : name is "Điện thoại" or "Hãng" or "Danh mục" ? "Chọn mục này để mở cửa sổ quản lý." : "Chưa triển khai";
                foreach (var item in navigation) item.BackColor = item == button ? Color.FromArgb(234, 240, 255) : Color.White;
                if(name=="Điện thoại"){using var form=new ProductListForm(api);form.ShowDialog(this);}
                else if(name is "Hãng" or "Danh mục"){using var form=new LookupForm(api,name=="Hãng"?"brands":"categories");form.ShowDialog(this);}
            };
        }
        var body = AdminTheme.Stack(32); body.BackColor = AdminTheme.Canvas;
        AdminTheme.Add(body, TitleLabel); AdminTheme.Add(body, AdminTheme.Label("Chào " + api.Identity?.Username + ". Phiên quản trị đang hoạt động."));
        var account = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill }; account.Controls.Add(PasswordButton); account.Controls.Add(LogoutButton); AdminTheme.Add(body, account);
        var panel = AdminTheme.Stack(28); panel.Dock = DockStyle.Top; panel.Margin = new Padding(0, 32, 0, 0);
        AdminTheme.Add(panel, ContentLabel); var note = AdminTheme.Label("Các chức năng quản lý sẽ được mở ở những bước tiếp theo.\nBạn có thể đổi mật khẩu hoặc đăng xuất bằng các nút phía trên."); note.MaximumSize = new Size(740, 0); AdminTheme.Add(panel, note); AdminTheme.Add(body, panel);
        root.Controls.Add(side, 0, 0); root.Controls.Add(body, 1, 0);
        StatusLabel.Text = ready.Environment + " · " + ready.Fingerprint + "   |   " + api.Identity?.Username + "   |   ADMIN"; StatusLabel.Padding = new Padding(16, 0, 0, 0); root.Controls.Add(StatusLabel, 0, 1); root.SetColumnSpan(StatusLabel, 2); Controls.Add(root);
        PasswordButton.Click += (_, _) => { using var form = new PasswordForm(api); form.ShowDialog(this); };
        LogoutButton.Click += async (_, _) => await LogoutAsync();
        FormClosing += async (_, args) => { if (ending) return; args.Cancel = true; ClosingApplication = true; await LogoutAsync(); };
    }
    public async Task LogoutAsync()
    {
        if (ending) return; ending = true; LogoutButton.Enabled = PasswordButton.Enabled = false; ContentLabel.Text = "Đang kết thúc phiên…";
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        try { await api.LogoutAsync(timeout.Token); }
        catch (Exception) { /* Best effort: local session always discarded by the client; never log response/credentials. */ }
        finally { LoggedOut?.Invoke(); if (!IsDisposed) Close(); }
    }
    public void EndSession() { ending = true; foreach (Form form in OwnedForms) form.Close(); Close(); }
}
