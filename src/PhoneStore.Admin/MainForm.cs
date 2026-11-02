using PhoneStore.Admin.Client;

namespace PhoneStore.Admin;

public sealed class MainForm : Form
{
    private readonly IAdminApi api;
    private bool ending;
    public DashboardView Dashboard { get; private set; }=null!;
    public bool ClosingApplication { get; private set; }
    public event Action? LoggedOut;
    public Label TitleLabel { get; } = AdminTheme.Label("Tổng quan", 23, true);
    public Label ContentLabel { get; } = AdminTheme.Label("Mở chức năng từ điều hướng bên trái.", 12);
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
        var brand = AdminTheme.Label("Monophone", 17, true); brand.AutoSize = false; brand.Size = new Size(190, 54); brand.ForeColor = Color.White; side.Controls.Add(brand);
        foreach (var name in new[] { "Tổng quan", "Điện thoại", "Khách hàng", "Nhập hàng", "Tồn kho", "Đơn hàng", "Báo cáo", "Hãng", "Danh mục", "Nhà cung cấp", "Tài khoản" })
        {
            var button = AdminTheme.Button(name, false); button.AutoSize=false;button.MinimumSize=new Size(160,36);button.Padding=new Padding(6,2,6,2);button.Margin=new Padding(0,5,0,0);button.Width = 190; button.Height = 40; navigation.Add(button); side.Controls.Add(button);
            button.Click += (_, _) =>
            {
                TitleLabel.Text = name; ContentLabel.Text = name == "Tài khoản" ? "Quản lý phiên và mật khẩu của bạn" : name is "Điện thoại" or "Hãng" or "Danh mục" or "Khách hàng" or "Nhà cung cấp" or "Nhập hàng" or "Tồn kho" or "Đơn hàng" ? "Chọn mục này để mở cửa sổ quản lý." : "Tải số liệu tổng quan bằng nút Tải tổng quan.";
                foreach (var item in navigation) item.BackColor = item == button ? Color.FromArgb(234, 240, 255) : Color.White;
                if(name=="Báo cáo"){using var form=new ReportScreen(api);form.ShowDialog(this);}
                else if(name=="Tổng quan"){TitleLabel.Text="Tổng quan";_ = Dashboard.LoadAsync();}
                else if(name=="Điện thoại"){using var form=new ProductListForm(api);form.ShowDialog(this);}
                else if(name is "Hãng" or "Danh mục"){using var form=new LookupForm(api,name=="Hãng"?"brands":"categories");form.ShowDialog(this);}
                else if(name=="Khách hàng"){using var form=new CustomerListForm(api);form.ShowDialog(this);}
                else if(name=="Nhà cung cấp"){using var form=new SupplierListForm(api);form.ShowDialog(this);}
                else if(name=="Đơn hàng"){using var form=new OrderListForm(api);form.ShowDialog(this);}
                else if(name=="Nhập hàng"){using var form=new PurchaseListForm(api);form.ShowDialog(this);}
                else if(name=="Tồn kho"){var settings=new StockPreferenceStore();using var form=new StockListForm(api,settings.Load(),settings.Save);form.ShowDialog(this);}
            };
        }
        var body=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=3,Padding=new Padding(16)};body.RowStyles.Add(new RowStyle(SizeType.AutoSize));body.RowStyles.Add(new RowStyle(SizeType.AutoSize));body.RowStyles.Add(new RowStyle(SizeType.Percent,100));
        var accountBar=CatalogUi.Toolbar(PasswordButton,LogoutButton);accountBar.Padding=new Padding(4);body.Controls.Add(TitleLabel,0,0);body.Controls.Add(accountBar,0,1);Dashboard=new(api);body.Controls.Add(Dashboard,0,2);Shown+=async(_,_)=>await Dashboard.LoadAsync();
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
