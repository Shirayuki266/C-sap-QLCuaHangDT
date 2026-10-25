using System.Text.Json;
using PhoneStore.Core.Contracts;
namespace PhoneStore.Admin;
public sealed class MainForm:Form
{
    private readonly ApiClient api;private readonly TabControl tabs=new(){Dock=DockStyle.Fill};private readonly Dictionary<TabPage,Func<Task>> loaders=[];private bool closedSession;
    public bool LoginAgain {get;private set;}
    public MainForm(ApiClient client,UserDto user,bool smoke=false) {
        api=client;Text=$"Phone Store · Quản trị cửa hàng · {user.Username}";Size=new(1360,900);MinimumSize=new(1280,800);StartPosition=FormStartPosition.CenterScreen;Font=new("Segoe UI",10);Controls.Add(tabs);
        var products=Products();var orders=Orders();var stock=Inventory();
        var dashboard=new ReportScreen(api,true,()=>{orders.Filter.SelectedValue="CHO_XAC_NHAN";tabs.SelectedTab=orders.Parent as TabPage;},()=>{stock.Search.Text="";stock.Tools.Controls.OfType<CheckBox>().First().Checked=true;tabs.SelectedTab=stock.Parent as TabPage;});
        AddTab("Tổng quan",dashboard,dashboard.RefreshData);AddTab("Điện thoại",products,products.RefreshData);AddLookup("Hãng","brands");AddLookup("Danh mục","categories");
        var customers=Customers();AddTab("Khách hàng",customers,customers.RefreshData);var suppliers=Suppliers();AddTab("Nhà cung cấp",suppliers,suppliers.RefreshData);var purchases=Purchases();AddTab("Nhập hàng",purchases,purchases.RefreshData);AddTab("Tồn kho",stock,stock.RefreshData);AddTab("Đơn hàng",orders,orders.RefreshData);
        var reports=new ReportScreen(api);AddTab("Báo cáo",reports,reports.RefreshData);AddAccount(user);
        if(!smoke){tabs.SelectedIndexChanged+=async(_,_)=>await LoadCurrent();Shown+=async(_,_)=>await LoadCurrent();}
        FormClosing+=async(_,e)=>{if(closedSession)return;e.Cancel=true;try{await api.Send<JsonElement>("/api/auth/logout",new{});}catch{}closedSession=true;Close();};
    }
    private void AddTab(string title,Control control,Func<Task>? load=null){var page=new TabPage(title);control.Dock=DockStyle.Fill;page.Controls.Add(control);tabs.TabPages.Add(page);if(load!=null)loaders[page]=load;}
    public async Task Smoke(string directory) {
        Directory.CreateDirectory(directory);File.WriteAllText(Path.Combine(directory,"smoke.txt"),"");if(File.Exists(Path.Combine(directory,"error.txt")))File.Delete(Path.Combine(directory,"error.txt"));CreateControl();var index=0;
        foreach(TabPage tab in tabs.TabPages){tabs.SelectedTab=tab;tab.CreateControl();if(loaders.TryGetValue(tab,out var load))await load();PerformLayout();foreach(Control control in tab.Controls){var failure=control.GetType().GetProperty("LastError")?.GetValue(control) as string;if(failure!=null)throw new InvalidOperationException(tab.Text+": "+failure);}using var bitmap=new Bitmap(Width,Height);DrawToBitmap(bitmap,new Rectangle(0,0,Width,Height));bitmap.Save(Path.Combine(directory,$"admin-{index++:00}.png"));File.AppendAllText(Path.Combine(directory,"smoke.txt"),"PASS "+tab.Text+Environment.NewLine);}
        var brandList=await api.Get<List<LookupDto>>("/api/admin/lookups/brands");var categories=await api.Get<List<LookupDto>>("/api/admin/lookups/categories");var product=(await api.Get<Page<ProductDto>>("/api/admin/products")).Items.First();
        void Capture(Form editor,string file){editor.ShowInTaskbar=false;editor.Opacity=0;editor.Show(this);Application.DoEvents();editor.PerformLayout();if(editor is ProductEditor&&(int)((ComboBox)editor.Controls.Find("BrandId",true).Single()).SelectedValue! != product.BrandId)throw new InvalidOperationException("Hãng không được giữ đúng trong form sửa sản phẩm.");using var bitmap=new Bitmap(editor.Width,editor.Height);editor.DrawToBitmap(bitmap,new Rectangle(0,0,editor.Width,editor.Height));bitmap.Save(Path.Combine(directory,file));editor.Close();}
        using(var editor=new ProductEditor(api,product,brandList,categories))Capture(editor,"product-editor.png");
        var receipt=(await api.Get<Page<PurchaseDto>>("/api/admin/purchases")).Items.First();
        using(var editor=new PurchaseEditor(api,await api.Get<List<SupplierDto>>("/api/admin/suppliers"),await api.Get<List<StockDto>>("/api/admin/stock"),receipt))Capture(editor,"purchase-editor.png");
        using(var editor=new EditDialog("Hồ sơ khách hàng",[new("FullName","Họ tên","Nguyễn Minh An"),new("Phone","Điện thoại","0901234567"),new("Address","Địa chỉ","123 Nguyễn Trãi, Quận 1",Multiline:true),new("Active","Tài khoản hoạt động",true)],_=>Task.CompletedTask))Capture(editor,"customer-editor.png");
    }
    private async Task LoadCurrent(){try{if(tabs.SelectedTab is { } page&&loaders.TryGetValue(page,out var load))await load();}catch(Exception e){MessageBox.Show(e.Message,"Lỗi tải dữ liệu");}}
    private static string Esc(string? s)=>Uri.EscapeDataString(s??"");
    private async Task EditProduct(ProductDto? p) {
        var brands=await api.Get<List<LookupDto>>("/api/admin/lookups/brands");var categories=await api.Get<List<LookupDto>>("/api/admin/lookups/categories");
        using var editor=new ProductEditor(api,p,brands,categories);if(editor.ShowDialog(this)!=DialogResult.OK)return;
        if(editor.InitialQuantities.Count>0){var saved=await api.Get<ProductDto>($"/api/admin/products/{editor.SavedId}");var initial=saved.Variants.Where(v=>editor.InitialQuantities.ContainsKey(v.Sku)).ToDictionary(v=>v.Id,v=>editor.InitialQuantities[v.Sku]);await EditPurchase(null,initial);}else MessageBox.Show("Đã lưu sản phẩm. Tồn chỉ tăng khi xác nhận phiếu nhập.","Thành công");
    }
    private ListScreen<ProductDto> Products() {
        var s=new ListScreen<ProductDto>();s.Loader=(page,query)=>api.Get<Page<ProductDto>>($"/api/admin/products?Query={Esc(query)}&Page={page}&PageSize=20");
        s.AddAction("Thêm điện thoại",_=>EditProduct(null),false);s.AddAction("Sửa / Biến thể",async p=>await EditProduct(await api.Get<ProductDto>($"/api/admin/products/{p!.Id}")));
        s.AddAction("Ngừng / Mở kinh doanh",async p=>{var current=await api.Get<ProductDto>($"/api/admin/products/{p!.Id}");if(!Ui.Confirm((current.Active?"Ngừng":"Mở lại")+" kinh doanh "+current.Name+"?"))return;await api.Send<JsonElement>($"/api/admin/products/{p.Id}",new ProductInput(current.Code,current.Name,current.BrandId,current.CategoryId,current.Specifications,current.Description,current.ImageUrl,!current.Active,current.Variants.Select(v=>new VariantInput(v.Id,v.Sku,v.Color,v.StorageGb,v.Price,v.Active,v.Version)).ToList()),HttpMethod.Put);});return s;
    }
    private void AddLookup(string title,string kind) {
        var s=new ListScreen<LookupDto>();s.Loader=async(page,q)=>{var list=await api.Get<List<LookupDto>>($"/api/admin/lookups/{kind}");list=list.Where(x=>x.Name.Contains(q,StringComparison.CurrentCultureIgnoreCase)).ToList();return new(list.Skip((page-1)*20).Take(20).ToList(),list.Count,page,20);};
        Task Edit(LookupDto? item){using var dialog=new EditDialog(title,[new("Name","Tên",item?.Name??""),new("Active","Đang sử dụng",item?.Active??true)],async d=>{if(item?.Active==true&&!d.Flag("Active")&&!Ui.Confirm("Ngừng sử dụng mục này? Sản phẩm thuộc mục này sẽ không được bán trên website."))throw new InvalidOperationException("Chưa xác nhận ngừng sử dụng.");await api.Send<JsonElement>(item==null?$"/api/admin/lookups/{kind}":$"/api/admin/lookups/{kind}/{item.Id}",new LookupRequest(d.Value("Name"),d.Flag("Active")),item==null?HttpMethod.Post:HttpMethod.Put);});dialog.ShowDialog(this);return Task.CompletedTask;}
        s.AddAction("Thêm",_=>Edit(null),false);s.AddAction("Sửa / Ngừng sử dụng",Edit);AddTab(title,s,s.RefreshData);
    }
    private ListScreen<CustomerDto> Customers() {
        var s=new ListScreen<CustomerDto>();s.Loader=(page,q)=>api.Get<Page<CustomerDto>>($"/api/admin/customers?query={Esc(q)}&page={page}");
        s.AddAction("Sửa / Khóa / Mở tài khoản",p=>{using var dialog=new EditDialog("Thông tin khách hàng",[new("FullName","Họ tên",p!.FullName),new("Phone","Điện thoại",p.Phone),new("Email","Email",p.Email),new("Address","Địa chỉ",p.Address,Multiline:true),new("Active","Tài khoản hoạt động",p.Active)],async d=>{if(p.Active&&!d.Flag("Active")&&!Ui.Confirm("Khóa tài khoản và kết thúc tất cả phiên đang đăng nhập?"))throw new InvalidOperationException("Chưa xác nhận khóa tài khoản.");await api.Send<bool>($"/api/admin/customers/{p.Id}",new CustomerInput(d.Value("FullName"),d.Value("Phone"),d.Value("Email"),d.Value("Address"),d.Flag("Active")),HttpMethod.Put);});dialog.ShowDialog(this);return Task.CompletedTask;});
        s.AddAction("Lịch sử đơn của khách",p=>{var list=new ListScreen<OrderDto>{Loader=(page,q)=>api.Get<Page<OrderDto>>($"/api/admin/orders?customerId={p!.Id}&query={Esc(q)}&page={page}")};list.AddAction("Xem / Xử lý",o=>{using var form=new OrderDetailForm(api,o!.Id);form.ShowDialog(this);return Task.CompletedTask;});using var window=new Form{Text="Đơn hàng · "+p!.FullName,Size=new(1200,800),StartPosition=FormStartPosition.CenterParent};window.Controls.Add(list);window.Shown+=async(_,_)=>await list.RefreshData();window.ShowDialog(this);return Task.CompletedTask;});return s;
    }
    private ListScreen<SupplierDto> Suppliers() {
        var s=new ListScreen<SupplierDto>();s.Loader=async(page,q)=>{var all=await api.Get<List<SupplierDto>>("/api/admin/suppliers?query="+Esc(q));return new(all.Skip((page-1)*20).Take(20).ToList(),all.Count,page,20);};
        Task Edit(SupplierDto? p){using var dialog=new EditDialog("Nhà cung cấp",[new("Code","Mã NCC",p?.Code??""),new("Name","Tên NCC",p?.Name??""),new("Phone","Điện thoại",p?.Phone),new("Address","Địa chỉ",p?.Address,Multiline:true),new("Active","Đang sử dụng",p?.Active??true)],async d=>{if(p?.Active==true&&!d.Flag("Active")&&!Ui.Confirm("Ngừng sử dụng nhà cung cấp này?"))throw new InvalidOperationException("Chưa xác nhận.");await api.Send<JsonElement>(p==null?"/api/admin/suppliers":$"/api/admin/suppliers/{p.Id}",new SupplierInput(d.Value("Code"),d.Value("Name"),d.Value("Phone"),d.Value("Address"),d.Flag("Active")),p==null?HttpMethod.Post:HttpMethod.Put);});dialog.ShowDialog(this);return Task.CompletedTask;}
        s.AddAction("Thêm NCC",_=>Edit(null),false);s.AddAction("Sửa / Ngừng sử dụng",Edit);return s;
    }
    private async Task EditPurchase(PurchaseDto? receipt,Dictionary<long,int>? initial=null) {
        using var editor=new PurchaseEditor(api,await api.Get<List<SupplierDto>>("/api/admin/suppliers"),await api.Get<List<StockDto>>("/api/admin/stock"),receipt,initial);editor.ShowDialog(this);
    }
    private ListScreen<PurchaseDto> Purchases() {
        var s=new ListScreen<PurchaseDto>();s.Filter.Visible=true;s.Filter.DisplayMember="Value";s.Filter.ValueMember="Key";s.Filter.DataSource=new Dictionary<string,string>{{"","Tất cả trạng thái"},{"NHAP","Nháp"},{"DA_XAC_NHAN","Đã xác nhận"},{"DA_HUY","Đã hủy"}}.ToList();
        s.Loader=(page,q)=>api.Get<Page<PurchaseDto>>($"/api/admin/purchases?query={Esc(q)}&status={s.Filter.SelectedValue}&page={page}");
        s.AddAction("Lập phiếu nhập",_=>EditPurchase(null),false);s.AddAction("Xem / Sửa nháp",async p=>await EditPurchase(await api.Get<PurchaseDto>($"/api/admin/purchases/{p!.Id}")));
        s.AddAction("Xác nhận nhập",async p=>{if(!Ui.Confirm($"Xác nhận phiếu {p!.Code}, tổng {p.Total.FormatMoney()} ₫, số lượng {p.Lines.Sum(x=>x.Quantity)}? Kho chỉ cộng một lần."))return;await api.Send<bool>($"/api/admin/purchases/{p.Id}/confirm",new TransitionRequest(p.Version,Guid.NewGuid()));});
        s.AddAction("Hủy phiếu nháp",async p=>{if(!Ui.Confirm("Hủy phiếu nháp "+p!.Code+"?"))return;await api.Send<bool>($"/api/admin/purchases/{p.Id}/cancel",new TransitionRequest(p.Version,Guid.NewGuid()));});return s;
    }
    private ListScreen<StockDto> Inventory() {
        var s=new ListScreen<StockDto>();var low=new CheckBox{Text="Chỉ sắp hết (khả dụng ≤ 3)",AutoSize=true};var brand=new ComboBox{Width=180,DropDownStyle=ComboBoxStyle.DropDownList};s.Tools.Controls.AddRange([low,new Label{Text="Hãng",AutoSize=true},brand]);bool loaded=false;
        s.Loader=async(page,q)=>{if(!loaded){var brands=await api.Get<List<LookupDto>>("/api/admin/lookups/brands");brands.Insert(0,new(0,"Tất cả hãng",true));brand.DataSource=brands;brand.DisplayMember="Name";brand.ValueMember="Id";loaded=true;}var list=await api.Get<List<StockDto>>($"/api/admin/stock?query={Esc(q)}&low={low.Checked.ToString().ToLowerInvariant()}&brandId={((int?)brand.SelectedValue>0?brand.SelectedValue:"")}");return new(list.Skip((page-1)*30).Take(30).ToList(),list.Count,page,30);};
        s.AddAction("Điều chỉnh kiểm kê",p=>{var key=Guid.NewGuid();using var d=new EditDialog("Kiểm kê · "+p!.Sku,[new("Counted","Số tồn thực tế kiểm kê",p.OnHand),new("Reason","Lý do bắt buộc","",Multiline:true)],async form=>{if(!Ui.Confirm($"Đổi tồn thực tế {p.OnHand} → {form.Int("Counted")}? Đang giữ {p.Reserved}."))throw new InvalidOperationException("Chưa xác nhận kiểm kê.");await api.Send<bool>($"/api/admin/stock/{p.VariantId}/adjust",new AdjustmentInput(form.Int("Counted"),form.Value("Reason"),p.Version,key));});d.ShowDialog(this);return Task.CompletedTask;});
        s.AddAction("Xem nhật ký",p=>{var logs=new ListScreen<MovementDto>{Loader=(page,q)=>api.Get<Page<MovementDto>>($"/api/admin/movements?variantId={p!.VariantId}&page={page}")};using var window=new Form{Text="Nhật ký kho · "+p!.Sku,Size=new(1200,750),StartPosition=FormStartPosition.CenterParent};window.Controls.Add(logs);window.Shown+=async(_,_)=>await logs.RefreshData();window.ShowDialog(this);return Task.CompletedTask;});return s;
    }
    private ListScreen<OrderDto> Orders() {
        var s=new ListScreen<OrderDto>();s.Filter.Visible=true;s.Filter.DisplayMember="Value";s.Filter.ValueMember="Key";s.Filter.DataSource=new Dictionary<string,string>{{"","Tất cả trạng thái"},{"CHO_XAC_NHAN","Chờ xác nhận"},{"DA_XAC_NHAN","Đã xác nhận"},{"DANG_GIAO","Đang giao"},{"HOAN_TAT","Hoàn tất"},{"DA_HUY","Đã hủy"},{"GIAO_THAT_BAI","Giao thất bại"}}.ToList();
        var from=new DateTimePicker{Format=DateTimePickerFormat.Short,ShowCheckBox=true,Checked=false,Width=150};var to=new DateTimePicker{Format=DateTimePickerFormat.Short,ShowCheckBox=true,Checked=false,Width=150};s.Tools.Controls.AddRange([new Label{Text="Từ ngày",AutoSize=true},from,new Label{Text="Đến ngày",AutoSize=true},to]);
        s.Loader=(page,q)=>api.Get<Page<OrderDto>>($"/api/admin/orders?query={Esc(q)}&status={s.Filter.SelectedValue}&from={(from.Checked?from.Value.ToString("yyyy-MM-dd"):"")}&to={(to.Checked?to.Value.ToString("yyyy-MM-dd"):"")}&page={page}");
        s.AddAction("Xem / Xử lý / In hóa đơn",p=>{using var detail=new OrderDetailForm(api,p!.Id);detail.ShowDialog(this);return Task.CompletedTask;});return s;
    }
    private void AddAccount(UserDto user) {
        var panel=new FlowLayoutPanel{Dock=DockStyle.Fill,Padding=new(28),FlowDirection=FlowDirection.TopDown};panel.Controls.Add(new Label{Text=$"Quản trị: {user.Username}\nMáy chủ: {api.BaseUrl}",AutoSize=true});
        var password=new Button{Text="Đổi mật khẩu quản trị",AutoSize=true};var logout=new Button{Text="Đăng xuất",AutoSize=true};panel.Controls.AddRange([password,logout]);
        password.Click+=async(_,_)=>{using var dialog=new EditDialog("Đổi mật khẩu",[new("Old","Mật khẩu hiện tại","",true),new("New","Mật khẩu mới (10+ ký tự, gồm chữ và số)","",true)],async d=>{await api.Send<bool>("/api/auth/password",new PasswordRequest(d.Value("Old"),d.Value("New")));});if(dialog.ShowDialog(this)==DialogResult.OK){closedSession=true;LoginAgain=true;Close();}await Task.CompletedTask;};
        logout.Click+=async(_,_)=>{logout.Enabled=false;try{await api.Send<JsonElement>("/api/auth/logout",new{});closedSession=true;LoginAgain=true;Close();}catch(Exception e){MessageBox.Show(e.Message,"Không thể đăng xuất");}finally{logout.Enabled=true;}};AddTab("Tài khoản",panel);
    }
}








