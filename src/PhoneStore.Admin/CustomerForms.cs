using PhoneStore.Admin.Client;

namespace PhoneStore.Admin;

public sealed class CustomerProfileEditor:Form
{
    private readonly IAdminApi api;
    private readonly ContactsWorkspace workspace;
    private readonly long id;
    private bool busy,loaded;
    public TextBox FullNameBox { get; }=AdminTheme.Input("Họ tên");
    public TextBox PhoneBox { get; }=AdminTheme.Input("Điện thoại");
    public TextBox EmailBox { get; }=AdminTheme.Input("Email");
    public TextBox AddressBox { get; }=AdminTheme.Input("Địa chỉ");
    public Button SaveButton { get; }=AdminTheme.Button("Lưu hồ sơ");
    public Button ReloadButton { get; }=AdminTheme.Button("Tải lại / đối soát",false);
    public Label MessageLabel { get; }=AdminTheme.Label("");
    public CustomerRecord? Saved { get; private set; }
    public CustomerProfileEditor(IAdminApi api,long customerId)
    {
        this.api=api;workspace=new(api);id=customerId;AdminTheme.Frame(this,"Sửa hồ sơ khách hàng");FullNameBox.MaxLength=120;PhoneBox.MaxLength=20;EmailBox.MaxLength=254;AddressBox.MaxLength=500;AddressBox.Multiline=true;AddressBox.Height=100;
        var body=AdminTheme.Stack(24);body.Dock=DockStyle.Top;AdminTheme.Add(body,AdminTheme.Label("Sửa hồ sơ khách hàng",22,true));
        AdminTheme.Field(body,"Họ tên *",FullNameBox);AdminTheme.Field(body,"Điện thoại *",PhoneBox);AdminTheme.Field(body,"Email · tùy chọn",EmailBox);AdminTheme.Field(body,"Địa chỉ",AddressBox);AdminTheme.Add(body,MessageLabel);AdminTheme.Add(body,CatalogUi.Toolbar(SaveButton,ReloadButton));Controls.Add(body);
        SaveButton.Enabled=false;SaveButton.Click+=async(_,_)=>await SaveAsync();ReloadButton.Click+=async(_,_)=>await LoadAsync();Shown+=async(_,_)=>await LoadAsync();
    }
    public CustomerProfileRequest Input()=>new(FullNameBox.Text,PhoneBox.Text,string.IsNullOrWhiteSpace(EmailBox.Text)?null:EmailBox.Text,AddressBox.Text);
    public async Task LoadAsync()
    {
        if(busy)return;if(loaded&&!api.NeedsReconciliation&&!CatalogUi.Confirm(this,"Tải lại hồ sơ","Các thay đổi chưa lưu sẽ được thay bằng dữ liệu máy chủ. Tiếp tục?"))return;SetBusy(true);
        try{var row=api.NeedsReconciliation?await workspace.ReconcileCustomer(id):await workspace.Customer(id);Bind(row);MessageLabel.Text="Đã tải hồ sơ. Chỉ sửa bốn trường thông tin liên hệ.";}
        catch(Exception error){MessageLabel.Text=AdminTheme.Failure(error);}finally{SetBusy(false);}
    }
    private void Bind(CustomerRecord row){Saved=row;loaded=true;FullNameBox.Text=row.FullName;PhoneBox.Text=row.Phone;EmailBox.Text=row.Email??"";AddressBox.Text=row.Address;}
    public async Task SaveAsync()
    {
        if(busy||!loaded)return;var request=Input();var error=ContactForms.ProfileError(request);if(error is not null){MessageLabel.Text=error;return;}SetBusy(true);
        try{Bind(await workspace.SaveProfile(id,request));MessageLabel.Text="Đã lưu hồ sơ. Thông tin người nhận ở đơn cũ được giữ nguyên.";}
        catch(Exception failure){MessageLabel.Text=AdminTheme.Failure(failure);}finally{SetBusy(false);}
    }
    private void SetBusy(bool value){busy=value;SaveButton.Enabled=!value&&loaded;ReloadButton.Enabled=FullNameBox.Enabled=PhoneBox.Enabled=EmailBox.Enabled=AddressBox.Enabled=!value;if(value)MessageLabel.Text="Đang xử lý…";}
}

public sealed class CustomerDetailsForm:Form
{
    private readonly IAdminApi api;
    private readonly ContactsWorkspace workspace;
    private readonly long id;
    private readonly Func<string,string,bool> confirm;
    private bool busy;
    public CustomerRecord? Current { get; private set; }
    public TextBox DetailsBox { get; }=new(){Multiline=true,ReadOnly=true,Dock=DockStyle.Fill,ScrollBars=ScrollBars.Vertical,AccessibleName="Hồ sơ khách hàng"};
    public Button EditButton { get; }=AdminTheme.Button("Sửa hồ sơ");
    public Button StateButton { get; }=AdminTheme.Button("Khóa / mở khách",false);
    public Button OrdersButton { get; }=AdminTheme.Button("Đơn của khách",false);
    public Button ReloadButton { get; }=AdminTheme.Button("Tải lại / đối soát",false);
    public Label MessageLabel { get; }=AdminTheme.Label("");
    public CustomerDetailsForm(IAdminApi api,long customerId,Func<string,string,bool>? confirmation=null)
    {
        this.api=api;workspace=new(api);id=customerId;confirm=confirmation??((t,m)=>CatalogUi.Confirm(this,t,m));AdminTheme.Frame(this,"Chi tiết khách hàng");
        var root=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=4,Padding=new Padding(24)};root.RowStyles.Add(new RowStyle(SizeType.AutoSize));root.RowStyles.Add(new RowStyle(SizeType.Percent,100));root.RowStyles.Add(new RowStyle(SizeType.AutoSize));root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.Controls.Add(AdminTheme.Label("Chi tiết khách hàng",22,true),0,0);root.Controls.Add(DetailsBox,0,1);root.Controls.Add(MessageLabel,0,2);root.Controls.Add(CatalogUi.Toolbar(EditButton,StateButton,OrdersButton,ReloadButton),0,3);Controls.Add(root);Actions();
        Shown+=async(_,_)=>await LoadAsync();ReloadButton.Click+=async(_,_)=>await LoadAsync();StateButton.Click+=async(_,_)=>await ChangeStateAsync();
        EditButton.Click+=async(_,_)=>{if(Current is not null){using var form=new CustomerProfileEditor(api,id);form.ShowDialog(this);await LoadAsync();}};
        OrdersButton.Click+=(_,_)=>{if(Current is not null){using var form=new CustomerOrdersForm(api,id);form.ShowDialog(this);}};
    }
    private void Bind(CustomerRecord row){Current=row;StateButton.Text=row.Active?"Khóa khách":"Mở khách";DetailsBox.Text=$"Tài khoản: {row.Username}\r\nHọ tên: {row.FullName}\r\nĐiện thoại: {row.Phone}\r\nEmail: {row.Email}\r\nĐịa chỉ: {row.Address}\r\nTrạng thái: {(row.Active?"Đang hoạt động":"Đã khóa")}\r\nNgày tạo: {AdminFormatting.Time(row.CreatedAt)}";}
    private void Actions(){StateButton.Visible=Current is not null;EditButton.Enabled=StateButton.Enabled=OrdersButton.Enabled=!busy&&Current is not null;ReloadButton.Enabled=!busy;}
    public async Task LoadAsync()
    {
        if(busy)return;busy=true;Actions();MessageLabel.Text="Đang tải…";
        try{Bind(api.NeedsReconciliation?await workspace.ReconcileCustomer(id):await workspace.Customer(id));MessageLabel.Text="Hồ sơ khách hàng từ máy chủ.";}
        catch(Exception error){Current=null;DetailsBox.Clear();MessageLabel.Text=AdminTheme.Failure(error);}finally{busy=false;Actions();}
    }
    public async Task ChangeStateAsync()
    {
        if(busy||Current is not {} row)return;var active=!row.Active;
        if(!confirm(active?"Mở khách hàng":"Khóa khách hàng",active?"Khách phải đăng nhập lại. Mở khóa không khôi phục các phiên cũ. Tiếp tục?":"Mọi phiên của khách sẽ bị thu hồi khi khóa. Dữ liệu hồ sơ và đơn được giữ nguyên. Tiếp tục?"))return;
        busy=true;Actions();MessageLabel.Text="Đang xử lý…";
        try{Bind(await workspace.SetCustomerState(row,active));MessageLabel.Text=active?"Đã mở. Khách phải đăng nhập lại; phiên cũ không được khôi phục.":"Đã khóa. Mọi phiên của khách đã bị thu hồi.";}
        catch(Exception error){MessageLabel.Text=AdminTheme.Failure(error);}finally{busy=false;Actions();}
    }
}

public sealed class CustomerOrdersForm:Form
{
    private readonly IAdminApi api;
    private readonly ContactsWorkspace workspace;
    private readonly long customerId;
    private int page=1,total;
    private bool busy;
    public DataGridView Grid { get; }=CatalogUi.Grid();
    public Button DetailsButton { get; }=AdminTheme.Button("Mở chi tiết đơn",false);
    public Button PreviousButton { get; }=AdminTheme.Button("Trang trước",false);
    public Button NextButton { get; }=AdminTheme.Button("Trang sau",false);
    public Label MessageLabel { get; }=AdminTheme.Label("");
    public event Action<OrderHandoff>? OrderRequested;
    public CustomerOrdersForm(IAdminApi api,long customerId)
    {
        this.api=api;workspace=new(api);this.customerId=customerId;AdminTheme.Frame(this,"Đơn của khách");Grid.ReadOnly=true;
        foreach(var (p,t) in new[]{("Code","Mã đơn"),("Status","Trạng thái"),("CreatedAt","Thời gian UTC+7"),("Total","Tổng tiền"),("Payment","COD")})CatalogUi.Column(Grid,p,t,true,180);
        CatalogUi.Layout(this,"Đơn của khách",CatalogUi.Toolbar(DetailsButton),Grid,CatalogUi.Toolbar(PreviousButton,NextButton,MessageLabel));
        Grid.SelectionChanged+=(_,_)=>Actions();Shown+=async(_,_)=>await LoadAsync();PreviousButton.Click+=async(_,_)=>{if(page>1){page--;await LoadAsync();}};NextButton.Click+=async(_,_)=>{if(page*20<total){page++;await LoadAsync();}};
        DetailsButton.Click+=(_,_)=>{if(Grid.CurrentRow?.Tag is CustomerOrder row){if(OrderRequested is not null)OrderRequested(new(row.Id));else{using var detail=new OrderPeekForm(api,row.Id);detail.ShowDialog(this);}}};
    }
    private void Actions(){DetailsButton.Enabled=!busy&&Grid.CurrentRow?.Tag is CustomerOrder;PreviousButton.Enabled=!busy&&page>1;NextButton.Enabled=!busy&&page*20<total;}
    public async Task LoadAsync()
    {
        if(busy)return;busy=true;Actions();MessageLabel.Text="Đang tải đơn…";
        try{var batch=await workspace.Orders(customerId,page);total=batch.Total;Grid.Rows.Clear();foreach(var row in batch.Items){var index=Grid.Rows.Add(row.Code,ContactForms.OrderState(row.Status),AdminFormatting.Time(row.CreatedAt),AdminFormatting.Money(row.Total),ContactForms.Payment(row.Payment));Grid.Rows[index].Tag=row;}MessageLabel.Text=total==0?"Khách chưa có đơn hàng.":$"Trang {page} · {total} đơn · Cửa sổ chi tiết chỉ xem thông tin, không đổi trạng thái đơn.";}
        catch(Exception error){Grid.Rows.Clear();MessageLabel.Text=AdminTheme.Failure(error);}finally{busy=false;Actions();}
    }
}

public sealed class OrderPeekForm:Form
{
    private readonly ContactsWorkspace workspace;
    private readonly long id;
    public TextBox DetailsBox { get; }=new(){Multiline=true,ReadOnly=true,Dock=DockStyle.Fill,ScrollBars=ScrollBars.Both,AccessibleName="Chi tiết đơn chỉ đọc"};
    public Label MessageLabel { get; }=AdminTheme.Label("");
    public OrderPeekForm(IAdminApi api,long orderId)
    {
        workspace=new(api);id=orderId;AdminTheme.Frame(this,"Chi tiết đơn · chỉ đọc");var root=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=3,Padding=new Padding(24)};root.RowStyles.Add(new RowStyle(SizeType.AutoSize));root.RowStyles.Add(new RowStyle(SizeType.Percent,100));root.RowStyles.Add(new RowStyle(SizeType.AutoSize));root.Controls.Add(AdminTheme.Label("Chi tiết đơn · chỉ đọc",22,true),0,0);root.Controls.Add(DetailsBox,0,1);root.Controls.Add(MessageLabel,0,2);Controls.Add(root);Shown+=async(_,_)=>await LoadAsync();
    }
    public async Task LoadAsync()
    {
        MessageLabel.Text="Đang tải đơn từ máy chủ…";
        try{var row=await workspace.Order(id);DetailsBox.Text=$"{row.Code} · {ContactForms.OrderState(row.Status)}\r\n{AdminFormatting.Time(row.CreatedAt)}\r\nNgười nhận: {row.Recipient}\r\nĐiện thoại: {row.Phone}\r\nĐịa chỉ: {row.Address}\r\nTổng tiền: {AdminFormatting.Money(row.Total)} · COD {ContactForms.Payment(row.Payment)}\r\n\r\n"+string.Join("\r\n",row.Lines.Select(l=>$"{l.Sku} · {l.Name} · {l.Quantity} × {AdminFormatting.Money(l.Price)} = {AdminFormatting.Money(l.Total)}"));MessageLabel.Text="Chỉ xem thông tin. Người nhận và giá được giữ theo thời điểm đặt hàng.";}
        catch(Exception error){DetailsBox.Clear();MessageLabel.Text=AdminTheme.Failure(error);}
    }
}
