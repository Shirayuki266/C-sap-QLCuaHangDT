using PhoneStore.Admin.Client;

namespace PhoneStore.Admin;

public sealed class MainForm : Form
{
    private readonly IAdminApi api;
    private bool ending;
    private readonly ToolTip tips = new();
    private readonly NavigationMouseFilter mouseFilter;
    private readonly FlowLayoutPanel breadcrumbs = new() { Dock = DockStyle.Fill, WrapContents = false, Padding = new(4), AutoScroll = true, Height = 52 };
    public DashboardView Dashboard { get; private set; } = null!;
    public bool ClosingApplication { get; private set; }
    public event Action? LoggedOut;
    public Label TitleLabel { get; } = AdminTheme.Label("Tổng quan", 16, true);
    public Label ContentLabel { get; } = AdminTheme.Label("Mở chức năng từ điều hướng bên trái.", 12);
    public Label StatusLabel { get; } = AdminTheme.Label("");
    public Button LogoutButton { get; } = AdminTheme.Button("Đăng &xuất", false);
    public Button PasswordButton { get; } = AdminTheme.Button("Đổi &mật khẩu", false);
    public Button BackButton { get; } = AdminTheme.Button("Quay lại", false);
    public Button ForwardButton { get; } = AdminTheme.Button("Tiến tới", false);
    public Button HomeButton { get; } = AdminTheme.Button("Tổng quan", false);
    public Button CloseAuxiliaryButton { get; } = AdminTheme.Button("Đóng các cửa sổ phụ", false);
    public Panel ContentHost { get; } = new() { Dock = DockStyle.Fill, AccessibleName = "Nội dung trang", Name = "ContentHost" };
    public NavigationService Navigator { get; }
    public IReadOnlyList<Button> Navigation => navigation;
    private readonly List<Button> navigation = [];
    public MainForm(IAdminApi api, ReadyInfo ready)
    {
        this.api = api; AdminTheme.Frame(this, "Quản trị cửa hàng");
        mouseFilter = new(this); Application.AddMessageFilter(mouseFilter);
        Navigator = new(this, ContentHost, () => { var page = new OverviewPage(api); Dashboard = page.Dashboard; return page; });
        Navigator.Changed += UpdateNavigation;
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2 };
        root.ColumnStyles.Add(new(SizeType.Absolute, 210)); root.ColumnStyles.Add(new(SizeType.Percent, 100));
        root.RowStyles.Add(new(SizeType.Percent, 100)); root.RowStyles.Add(new(SizeType.Absolute, 36));
        var side = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true, BackColor = AdminTheme.Navy, Padding = new(10) };
        var brand = AdminTheme.Label("Monophone", 16, true); brand.MinimumSize = new(180, 48); brand.ForeColor = Color.White; side.Controls.Add(brand);
        foreach (var name in new[] { "Tổng quan", "Điện thoại", "Khách hàng", "Nhập hàng", "Tồn kho", "Đơn hàng", "Báo cáo", "Hãng", "Danh mục", "Nhà cung cấp", "Tài khoản" })
        {
            var button = AdminTheme.Button(name, false); button.AutoSize = true; button.MinimumSize = new(180, 40);
            button.Padding = new(6, 2, 6, 2); button.Margin = new(0, 4, 0, 0); button.Size = new(180, 40);
            navigation.Add(button); side.Controls.Add(button);
            button.Click += async (_, _) =>
            {
                if (name == "Tổng quan") { await Navigator.HomeAsync(); return; }
                if (name == "Tài khoản") { TitleLabel.Text = name; ContentLabel.Text = "Quản lý phiên và mật khẩu của bạn"; PasswordButton.Focus(); return; }
                NavigationPageForm page = name switch
                {
                    "Điện thoại" => new ProductListForm(api), "Khách hàng" => new CustomerListForm(api),
                    "Nhập hàng" => new PurchaseListForm(api), "Tồn kho" => StockPage(), "Đơn hàng" => new OrderListForm(api),
                    "Báo cáo" => new ReportScreen(api), "Nhà cung cấp" => new SupplierListForm(api),
                    _ => new LookupForm(api, name == "Hãng" ? "brands" : "categories")
                };
                await Navigator.OpenAsync(page);
            };
        }
        var body = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Padding = new(8) };
        body.RowStyles.Add(new(SizeType.AutoSize)); body.RowStyles.Add(new(SizeType.Absolute, 52));
        body.RowStyles.Add(new(SizeType.Percent, 100));
        var navBar = CatalogUi.Toolbar(BackButton, ForwardButton, HomeButton, PasswordButton, LogoutButton, CloseAuxiliaryButton); navBar.Padding = new(0);
        foreach (var button in new[] { BackButton, ForwardButton, HomeButton, PasswordButton, LogoutButton, CloseAuxiliaryButton })
        { button.MinimumSize = new(80, 32); button.Padding = new(6, 2, 6, 2); button.Margin = new(0, 0, 6, 2); button.AccessibleName = button.Text.Replace("&", ""); }
        tips.SetToolTip(BackButton, "Quay lại (Alt+←)"); tips.SetToolTip(ForwardButton, "Tiến tới (Alt+→)");
        tips.SetToolTip(HomeButton, "Về Tổng quan, xóa lịch sử (Alt+Home / Ctrl+H)");
        tips.SetToolTip(CloseAuxiliaryButton, "Đóng các cửa sổ phụ không modal");
        body.Controls.Add(navBar, 0, 0); body.Controls.Add(breadcrumbs, 0, 1); body.Controls.Add(ContentHost, 0, 2);
        root.Controls.Add(side, 0, 0); root.Controls.Add(body, 1, 0);
        StatusLabel.Text = ready.Environment + " · " + ready.Fingerprint + "   |   " + api.Identity?.Username + "   |   ADMIN";
        StatusLabel.Padding = new(12, 0, 0, 0); root.Controls.Add(StatusLabel, 0, 1); root.SetColumnSpan(StatusLabel, 2); Controls.Add(root);
        BackButton.Click += async (_, _) => await Navigator.BackAsync(); ForwardButton.Click += async (_, _) => await Navigator.ForwardAsync();
        HomeButton.Click += async (_, _) => await Navigator.HomeAsync();
        CloseAuxiliaryButton.Click += async (_, _) => { foreach (var form in OwnedForms.Where(f => !f.Modal).ToArray()) if (await UnsavedChanges.TryLeaveAsync(form)) UnsavedChanges.DiscardAndClose(form); };
        PasswordButton.Click += (_, _) => { using var form = new PasswordForm(api); form.ShowDialog(this); };
        LogoutButton.Click += async (_, _) => await LogoutAsync();
        Load += async (_, _) => await Navigator.HomeAsync();
        FormClosing += async (_, args) =>
        {
            if (ending) return; args.Cancel = true;
            if (Navigator.CurrentPage is { } page && !await UnsavedChanges.TryLeaveAsync(page)) return;
            ClosingApplication = true; await LogoutAsync(skipGuard: true);
        };
        Disposed += (_, _) => { Application.RemoveMessageFilter(mouseFilter); Navigator.Dispose(); tips.Dispose(); };
        UpdateNavigation();
    }
    private StockListForm StockPage() { var settings = new StockPreferenceStore(); return new(api, settings.Load(), settings.Save); }
    internal void RefreshNavigationState() => UpdateNavigation();
    private void UpdateNavigation()
    {
        if (IsDisposed) return;
        var history = Navigator.History; var enabled = !ending && !Navigator.IsEnded && !Navigator.IsBusy;
        BackButton.Enabled = enabled && history.CanBack; ForwardButton.Enabled = enabled && history.CanForward;
        HomeButton.Enabled = PasswordButton.Enabled = CloseAuxiliaryButton.Enabled = enabled;
        LogoutButton.Enabled = !ending && !Navigator.IsEnded;
        foreach (var button in navigation) button.Enabled = enabled;
        breadcrumbs.SuspendLayout();
        foreach (Control child in breadcrumbs.Controls.Cast<Control>().ToArray()) child.Dispose();
        for (var index = 0; index <= history.Index; index++)
        {
            var target = index; var title = NavigationService.Title(history.Entries[index]);
            var crumb = new LinkLabel { Text = (index == 0 ? "" : "› ") + title, AutoEllipsis = true, Width = Math.Min(220, TextRenderer.MeasureText(title + "› ", Font).Width + 12), Height = Font.Height + 8, Margin = new(0, 0, 4, 0), AccessibleName = "Đi đến " + title, Enabled = enabled && target != history.Index };
            crumb.FontChanged += (_, _) => crumb.Height = crumb.Font.Height + 8;
            tips.SetToolTip(crumb, "Đi đến " + title); crumb.LinkClicked += async (_, _) => await Navigator.GoToAsync(target); breadcrumbs.Controls.Add(crumb);
        }
        breadcrumbs.ResumeLayout();
        if (breadcrumbs.Controls.Count > 0) breadcrumbs.ScrollControlIntoView(breadcrumbs.Controls[^1]);
        if (Navigator.CurrentPage is { } current) TitleLabel.Text = NavigationService.Title(current);
    }
    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (!OwnedForms.Any(f => f.Visible && f.Modal))
        {
            if (keyData == (Keys.Alt | Keys.Left)) { _ = Navigator.BackAsync(); return true; }
            if (keyData == (Keys.Alt | Keys.Right)) { _ = Navigator.ForwardAsync(); return true; }
            if (keyData == (Keys.Alt | Keys.Home) || keyData == (Keys.Control | Keys.H)) { _ = Navigator.HomeAsync(); return true; }
            if (keyData == (Keys.Control | Keys.S) && Navigator.CurrentPage?.AcceptButton is Button save && save.Enabled && save.Text.StartsWith("Lưu", StringComparison.Ordinal)) { save.PerformClick(); return true; }
        }
        return base.ProcessCmdKey(ref msg, keyData);
    }
    protected override void WndProc(ref Message m)
    {
        if (m.Msg == 0x020B) { var button = (m.WParam.ToInt64() >> 16) & 0xffff; if (button == 1) _ = Navigator.BackAsync(); if (button == 2) _ = Navigator.ForwardAsync(); m.Result = new(1); return; }
        base.WndProc(ref m);
    }
    public async Task LogoutAsync(bool skipGuard = false)
    {
        if (ending) return;
        if (!skipGuard && Navigator.CurrentPage is { } page && !await UnsavedChanges.TryLeaveAsync(page)) return;
        ending = true; Navigator.Dispose(); UpdateNavigation(); ContentLabel.Text = "Đang kết thúc phiên…";
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        try { await api.LogoutAsync(timeout.Token); } catch (Exception) { /* Local session is always discarded. */ }
        finally { LoggedOut?.Invoke(); if (!IsDisposed) Close(); }
    }
    public void EndSession()
    {
        ending = true; Navigator.Dispose();
        void CloseOwned(Form owner) { foreach (var form in owner.OwnedForms.ToArray()) { CloseOwned(form); UnsavedChanges.DiscardAndClose(form); } }
        CloseOwned(this); Close();
    }
    private sealed class NavigationMouseFilter(MainForm shell) : IMessageFilter
    {
        public bool PreFilterMessage(ref Message m)
        {
            if (m.Msg != 0x020B || shell.IsDisposed || PageNavigation.Shell(Control.FromHandle(m.HWnd)) != shell) return false;
            var button = (m.WParam.ToInt64() >> 16) & 0xffff;
            if (button == 1) _ = shell.Navigator.BackAsync(); else if (button == 2) _ = shell.Navigator.ForwardAsync(); else return false;
            return true;
        }
    }
}
