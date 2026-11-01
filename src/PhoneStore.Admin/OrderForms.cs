using PhoneStore.Admin.Client;

namespace PhoneStore.Admin;

public sealed class OrderListForm:Form
{
    private readonly IAdminApi api;private readonly OrdersWorkspace workspace;private bool busy;private int page=1,total;
    public TextBox QueryBox { get; }=AdminTheme.Input("Tìm mã đơn, người nhận, khách hoặc điện thoại");
    public ComboBox StateBox { get; }=new(){DropDownStyle=ComboBoxStyle.DropDownList,Width=175,AccessibleName="Trạng thái đơn"};
    public DateTimePicker FromBox { get; }=DateInput("Từ ngày UTC+7");public DateTimePicker ToBox { get; }=DateInput("Đến hết ngày UTC+7");
    public Button SearchButton { get; }=AdminTheme.Button("Tìm kiếm");public Button DetailsButton { get; }=AdminTheme.Button("Mở chi tiết",false);public Button PreviousButton { get; }=AdminTheme.Button("Trang trước",false);public Button NextButton { get; }=AdminTheme.Button("Trang sau",false);
    public DataGridView Grid { get; }=CatalogUi.Grid();public Label MessageLabel { get; }=AdminTheme.Label("");
    private static DateTimePicker DateInput(string name)=>new(){Format=DateTimePickerFormat.Custom,CustomFormat="dd/MM/yyyy",ShowCheckBox=true,Checked=false,Width=150,AccessibleName=name};
    public OrderListForm(IAdminApi api)
    {
        this.api=api;workspace=new(api);AdminTheme.Frame(this,"Đơn hàng");QueryBox.Dock=DockStyle.None;QueryBox.Width=270;QueryBox.MaxLength=100;QueryBox.PlaceholderText="Mã, người nhận, khách, điện thoại";StateBox.Items.Add("Tất cả trạng thái");StateBox.Items.AddRange(OrderPresentation.States.Select(ContactForms.OrderState).ToArray());StateBox.SelectedIndex=0;Grid.ReadOnly=true;
        foreach(var(p,t)in new[]{("Code","Mã đơn"),("Recipient","Người nhận"),("Phone","Điện thoại"),("Status","Trạng thái đơn"),("Payment","COD"),("CreatedAt","Đặt lúc UTC+7"),("Total","Tổng tiền")})CatalogUi.Column(Grid,p,t,true,180);
        CatalogUi.Layout(this,"Đơn hàng",CatalogUi.Toolbar(QueryBox,StateBox,AdminTheme.Label("Từ ngày"),FromBox,AdminTheme.Label("Đến hết ngày"),ToBox,SearchButton,DetailsButton),Grid,CatalogUi.Toolbar(PreviousButton,NextButton,MessageLabel));
        SearchButton.Click+=async(_,_)=>{if(busy)return;page=1;await LoadAsync();};PreviousButton.Click+=async(_,_)=>{if(!busy&&page>1){page--;await LoadAsync();}};NextButton.Click+=async(_,_)=>{if(!busy&&page*20<total){page++;await LoadAsync();}};DetailsButton.Click+=async(_,_)=>{if(Grid.CurrentRow?.Tag is AdminOrder row){using var form=new OrderDetailForm(api,row.Id);form.ShowDialog(this);await LoadAsync();}};Grid.SelectionChanged+=(_,_)=>Actions();Shown+=async(_,_)=>await LoadAsync();AcceptButton=SearchButton;Actions();
    }
    private void Actions(){SearchButton.Enabled=QueryBox.Enabled=StateBox.Enabled=FromBox.Enabled=ToBox.Enabled=Grid.Enabled=!busy;DetailsButton.Enabled=!busy&&Grid.CurrentRow?.Tag is AdminOrder;PreviousButton.Enabled=!busy&&page>1;NextButton.Enabled=!busy&&page*20<total;}
    public async Task LoadAsync()
    {
        if(busy)return;busy=true;Actions();MessageLabel.Text="Đang tải đơn hàng…";
        try{var rows=await workspace.List(QueryBox.Text,StateBox.SelectedIndex>0?OrderPresentation.States[StateBox.SelectedIndex-1]:null,FromBox.Checked?DateOnly.FromDateTime(FromBox.Value):null,ToBox.Checked?DateOnly.FromDateTime(ToBox.Value):null,page);total=rows.Total;Grid.Rows.Clear();foreach(var row in rows.Items){var i=Grid.Rows.Add(row.Code,row.Recipient,row.Phone,ContactForms.OrderState(row.Status),ContactForms.Payment(row.Payment),AdminFormatting.Time(row.CreatedAt),AdminFormatting.Money(row.Total));Grid.Rows[i].Tag=row;}MessageLabel.Text=total==0?"Không có đơn phù hợp. Thử đổi từ khóa hoặc bộ lọc.":$"Trang {page} · {total} đơn · Ngày cuối được bao gồm, theo UTC+7.";}
        catch(Exception error){Grid.Rows.Clear();total=0;MessageLabel.Text=AdminTheme.Failure(error);}finally{busy=false;Actions();}
    }
}

public sealed class OrderDetailForm:Form
{
    private readonly IAdminApi api;private readonly OrdersWorkspace workspace;private readonly long id;private readonly Func<string,string,bool> confirm;private bool busy;private OrderOperation? operation;
    public AdminOrder? Current { get; private set; }
    public TextBox SummaryBox { get; }=new(){Multiline=true,ReadOnly=true,Dock=DockStyle.Fill,ScrollBars=ScrollBars.Vertical,Height=155,AccessibleName="Người nhận, trạng thái đơn và COD"};
    public DataGridView LinesGrid { get; }=CatalogUi.Grid();public DataGridView HistoryGrid { get; }=CatalogUi.Grid();
    public TextBox ReasonBox { get; }=AdminTheme.Input("Lý do xử lý đơn");public Label ReasonErrorLabel { get; }=AdminTheme.Label("");public Label MessageLabel { get; }=AdminTheme.Label("");public Button ReloadButton { get; }=AdminTheme.Button("Tải lại / đối soát",false);
    public IReadOnlyDictionary<OrderAction,Button> ActionButtons=>buttons;private readonly Dictionary<OrderAction,Button> buttons=[];
    public Guid? OperationKey=>operation?.Intent.Key;
    public OrderDetailForm(IAdminApi api,long orderId,Func<string,string,bool>? confirmation=null)
    {
        this.api=api;workspace=new(api);id=orderId;confirm=confirmation??((t,m)=>CatalogUi.Confirm(this,t,m));AdminTheme.Frame(this,"Chi tiết đơn hàng");ReasonBox.MaxLength=1000;LinesGrid.ReadOnly=HistoryGrid.ReadOnly=true;
        foreach(var(p,t)in new[]{("Sku","SKU"),("Name","Điện thoại snapshot"),("Description","Phiên bản snapshot"),("Quantity","Số lượng"),("Price","Đơn giá"),("Total","Thành tiền")})CatalogUi.Column(LinesGrid,p,t,true,p=="Name"?250:170);
        foreach(var(p,t)in new[]{("At","Thời gian UTC+7"),("From","Từ"),("To","Đến"),("Actor","Người xử lý"),("Reason","Lý do")})CatalogUi.Column(HistoryGrid,p,t,true,p=="Reason"?350:210);
        var tabs=new TabControl{Dock=DockStyle.Fill};var items=new TabPage("Dòng hàng snapshot");items.Controls.Add(LinesGrid);var history=new TabPage("Lịch sử xử lý");history.Controls.Add(HistoryGrid);tabs.TabPages.AddRange([items,history]);
        var actions=new List<Control>();foreach(var action in Enum.GetValues<OrderAction>().Where(a=>a!=OrderAction.CustomerCancel)){var button=AdminTheme.Button(OrderPresentation.Label(action),action is not(OrderAction.Cancel or OrderAction.Fail));buttons.Add(action,button);actions.Add(button);button.Click+=async(_,_)=>{if(action==OrderAction.Invoice)await OpenInvoiceAsync();else await ActAsync(action);};}actions.Add(ReloadButton);
        var root=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=8,Padding=new Padding(20)};root.RowStyles.Add(new RowStyle(SizeType.AutoSize));root.RowStyles.Add(new RowStyle(SizeType.Absolute,155));root.RowStyles.Add(new RowStyle(SizeType.Percent,100));for(var i=3;i<8;i++)root.RowStyles.Add(new RowStyle(SizeType.AutoSize));root.Controls.Add(AdminTheme.Label("Chi tiết đơn hàng",22,true),0,0);root.Controls.Add(SummaryBox,0,1);root.Controls.Add(tabs,0,2);root.Controls.Add(AdminTheme.Label("Lý do · bắt buộc khi hủy/giao thất bại; nhận lại tùy chọn"),0,3);root.Controls.Add(ReasonBox,0,4);root.Controls.Add(ReasonErrorLabel,0,5);root.Controls.Add(MessageLabel,0,6);root.Controls.Add(CatalogUi.Toolbar(actions.ToArray()),0,7);Controls.Add(root);
        ReloadButton.Click+=async(_,_)=>await LoadAsync();Shown+=async(_,_)=>await LoadAsync();Actions();
    }
    private void Bind(AdminOrder row){Current=row;SummaryBox.Text=OrderPresentation.Summary(row);LinesGrid.Rows.Clear();foreach(var line in row.Lines)LinesGrid.Rows.Add(line.Sku,line.Name,line.Description,line.Quantity,AdminFormatting.Money(line.Price),AdminFormatting.Money(line.Total));HistoryGrid.Rows.Clear();foreach(var h in row.History)HistoryGrid.Rows.Add(AdminFormatting.Time(h.At),h.From is null?"Đặt mới":ContactForms.OrderState(h.From),ContactForms.OrderState(h.To),h.Actor,h.Reason??"");}
    private void Actions(){var pending=operation?.NeedsReadback==true||api.NeedsReconciliation;foreach(var(action,button)in buttons){button.Visible=Current is{} row&&OrderPresentation.Allowed(row.Status,row.ReturnReceived,action);button.Enabled=!busy&&!pending;}ReloadButton.Enabled=!busy;ReasonBox.Enabled=!busy&&!pending&&Current is not null;}
    public async Task LoadAsync()
    {
        if(busy)return;busy=true;Actions();MessageLabel.Text="Đang tải / đối soát đơn…";
        try{if(operation is not null)Bind(await workspace.Resolve(operation));else if(api.NeedsReconciliation){AdminOrder row=null!;await api.ReconcileAsync(async token=>row=await workspace.Read(id,token));Bind(row);}else Bind(await workspace.Read(id));MessageLabel.Text="Thông tin và giá snapshot từ máy chủ.";}
        catch(Exception error){Current=null;SummaryBox.Clear();LinesGrid.Rows.Clear();HistoryGrid.Rows.Clear();MessageLabel.Text=AdminTheme.Failure(error);}finally{busy=false;Actions();}
    }
    public async Task ActAsync(OrderAction action)
    {
        if(busy||Current is null||operation?.NeedsReadback==true||api.NeedsReconciliation)return;var invalid=OrderPresentation.ReasonError(action,ReasonBox.Text);ReasonErrorLabel.Text=invalid??"";if(invalid is not null){ReasonBox.Focus();return;}
        var candidate=operation is{} old&&!old.Finished&&old.Action==action&&old.Reason==(string.IsNullOrWhiteSpace(ReasonBox.Text)?null:ReasonBox.Text.Trim())?old:new OrderOperation(Current,action,ReasonBox.Text);
        if(!confirm(OrderPresentation.Label(action),OrderPresentation.Confirmation(candidate)))return;operation=candidate;busy=true;Actions();MessageLabel.Text="Đang xử lý…";
        try{Bind(await workspace.Execute(operation));ReasonBox.Clear();MessageLabel.Text="Đã xử lý. Trạng thái và COD đã tải lại từ máy chủ.";}
        catch(Exception error)
        {
            var message=AdminTheme.Failure(error);if(error is AdminApiException{Status:409}){try{Bind(await workspace.Resolve(operation));message+=" Đã tải lại dữ liệu mới; hãy kiểm tra trước khi thực hiện tiếp.";}catch(Exception){message+=" Chưa tải được bản mới. Chọn Tải lại / đối soát.";}}MessageLabel.Text=message;
        }
        finally{busy=false;Actions();}
    }
    public Task OpenInvoiceAsync(){if(busy||Current is null)return Task.CompletedTask;using var form=new InvoiceForm(api,id);form.ShowDialog(this);return Task.CompletedTask;}
}
