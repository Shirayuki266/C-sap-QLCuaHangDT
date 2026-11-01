using System.ComponentModel;
using PhoneStore.Admin.Client;

namespace PhoneStore.Admin;

public sealed class PurchaseListForm:Form
{
    private readonly IAdminApi api;private readonly SupplyWorkspace workspace;private int page=1,total;private bool busy;
    public TextBox QueryBox { get; }=AdminTheme.Input("Tìm mã phiếu hoặc tên NCC");
    public ComboBox StateBox { get; }=new(){DropDownStyle=ComboBoxStyle.DropDownList,Width=180,AccessibleName="Trạng thái phiếu"};
    public Button SearchButton { get; }=AdminTheme.Button("Tìm kiếm");public Button NewButton { get; }=AdminTheme.Button("Lập phiếu nhập");public Button OpenButton { get; }=AdminTheme.Button("Mở phiếu",false);
    public Button PreviousButton { get; }=AdminTheme.Button("Trang trước",false);public Button NextButton { get; }=AdminTheme.Button("Trang sau",false);
    public DataGridView Grid { get; }=CatalogUi.Grid();public Label MessageLabel { get; }=AdminTheme.Label("");
    public PurchaseListForm(IAdminApi api)
    {
        this.api=api;workspace=new(api);AdminTheme.Frame(this,"Nhập hàng");QueryBox.Dock=DockStyle.None;QueryBox.Width=300;QueryBox.MaxLength=100;QueryBox.PlaceholderText="Mã phiếu hoặc tên NCC";StateBox.Items.AddRange(["Tất cả trạng thái","Nháp","Đã xác nhận","Đã hủy"]);StateBox.SelectedIndex=0;Grid.ReadOnly=true;
        foreach(var(p,t)in new[]{("Code","Mã phiếu"),("Supplier","Nhà cung cấp"),("Status","Trạng thái"),("CreatedAt","Ngày lập UTC+7"),("Total","Tổng tiền")})CatalogUi.Column(Grid,p,t,true,p=="Supplier"?260:180);
        CatalogUi.Layout(this,"Nhập hàng",CatalogUi.Toolbar(QueryBox,StateBox,SearchButton,NewButton,OpenButton),Grid,CatalogUi.Toolbar(PreviousButton,NextButton,MessageLabel));
        SearchButton.Click+=async(_,_)=>{page=1;await LoadAsync();};NewButton.Click+=async(_,_)=>{using var f=new PurchaseEditor(api);f.ShowDialog(this);await LoadAsync();};OpenButton.Click+=async(_,_)=>{if(Grid.CurrentRow?.Tag is PurchaseRecord p){using var f=new PurchaseEditor(api,p.Id);f.ShowDialog(this);await LoadAsync();}};
        Grid.SelectionChanged+=(_,_)=>Actions();PreviousButton.Click+=async(_,_)=>{if(page>1){page--;await LoadAsync();}};NextButton.Click+=async(_,_)=>{if(page*20<total){page++;await LoadAsync();}};Shown+=async(_,_)=>await LoadAsync();AcceptButton=SearchButton;
    }
    private void Actions(){OpenButton.Enabled=!busy&&Grid.CurrentRow?.Tag is PurchaseRecord;SearchButton.Enabled=NewButton.Enabled=QueryBox.Enabled=StateBox.Enabled=Grid.Enabled=!busy;PreviousButton.Enabled=!busy&&page>1;NextButton.Enabled=!busy&&page*20<total;}
    public async Task LoadAsync()
    {
        if(busy)return;busy=true;Actions();MessageLabel.Text="Đang tải phiếu…";
        try{var status=StateBox.SelectedIndex switch{1=>"NHAP",2=>"DA_XAC_NHAN",3=>"DA_HUY",_=>null};var batch=await workspace.Purchases(QueryBox.Text,status,page);total=batch.Total;Grid.Rows.Clear();foreach(var p in batch.Items){var i=Grid.Rows.Add(p.Code,p.Supplier,SupplyPresentation.State(p.Status),AdminFormatting.Time(p.CreatedAt),AdminFormatting.Money(p.Total));Grid.Rows[i].Tag=p;}MessageLabel.Text=total==0?"Chưa có phiếu phù hợp. Thử đổi từ khóa hoặc trạng thái.":$"Trang {page} · {total} phiếu · 20 dòng/trang";}
        catch(Exception error){Grid.Rows.Clear();MessageLabel.Text=SupplyPresentation.Failure(error);}finally{busy=false;Actions();}
    }
}

public sealed class PurchaseEditor:Form
{
    private readonly IAdminApi api;private readonly SupplyWorkspace workspace;private readonly ContactsWorkspace contacts;private readonly PurchaseDraftSession session;private readonly PurchaseHandoff? handoff;
    private readonly Func<string,string,bool> confirm;private PurchaseOperation? operation;private bool busy,loaded,binding;private List<SupplierRecord> suppliers=[];
    public PurchaseRecord? Current=>session.Current;public Guid RequestKey=>session.RequestKey;
    public BindingList<PurchaseLineDraft> Lines { get; private set; }=new();
    public ComboBox SupplierBox { get; }=new(){DropDownStyle=ComboBoxStyle.DropDownList,Dock=DockStyle.Fill,AccessibleName="NCC hoạt động"};
    public Label SummaryLabel { get; }=AdminTheme.Label("Phiếu mới · Tổng tiền sẽ có sau khi lưu nháp.");public Label MessageLabel { get; }=AdminTheme.Label("");public Label ConfirmHintLabel { get; }=AdminTheme.Label("");
    public DataGridView LinesGrid { get; }=CatalogUi.Grid();public DataGridView ImpactGrid { get; }=CatalogUi.Grid();
    public Button AddButton { get; }=AdminTheme.Button("Thêm biến thể",false);public Button RemoveButton { get; }=AdminTheme.Button("Bỏ dòng",false);public Button SaveButton { get; }=AdminTheme.Button("Lưu nháp");
    public Button ConfirmButton { get; }=AdminTheme.Button("Xác nhận phiếu");public Button CancelDraftButton { get; }=AdminTheme.Button("Hủy phiếu nháp",false);public Button ReloadButton { get; }=AdminTheme.Button("Tải lại / đối soát",false);
    public PurchaseEditor(IAdminApi api,long id=0,PurchaseHandoff? handoff=null,Func<string,string,bool>? confirmation=null)
    {
        this.api=api;workspace=new(api);contacts=new(api);session=new(api,id);this.handoff=handoff;confirm=confirmation??((t,m)=>CatalogUi.Confirm(this,t,m));AdminTheme.Frame(this,"Phiếu nhập hàng");
        CatalogUi.Column(LinesGrid,"Sku","SKU",true,160);CatalogUi.Column(LinesGrid,"Name","Điện thoại",true,280);CatalogUi.Column(LinesGrid,"Quantity","Số lượng *",false,140);CatalogUi.Column(LinesGrid,"Cost","Giá vốn nhập (đ) *",false,180);
        LinesGrid.DataSource=Lines;LinesGrid.DataError+=(_,e)=>{e.ThrowException=false;MessageLabel.Text="Số lượng phải là số nguyên; giá vốn là số dương. Hãy sửa ô đang nhập.";};
        foreach(var(p,t)in new[]{("Sku","SKU"),("Cost","Giá vốn hiện tại (đ)"),("Price","Giá bán hiện tại (đ)"),("OnHand","Tồn thực"),("Reserved","Đang giữ"),("Available","Có thể bán")})CatalogUi.Column(ImpactGrid,p,t,true,170);ImpactGrid.ReadOnly=true;
        var tabs=new TabControl{Dock=DockStyle.Fill};var detail=new TabPage("Dòng nhập");var impact=new TabPage("Tồn và giá từ máy chủ");tabs.TabPages.AddRange([detail,impact]);detail.Controls.Add(LinesGrid);impact.Controls.Add(ImpactGrid);
        var top=new TableLayoutPanel{AutoSize=true,Dock=DockStyle.Fill,ColumnCount=1};top.Controls.Add(AdminTheme.Label("Phiếu nhập hàng",22,true));top.Controls.Add(AdminTheme.Label("Nhà cung cấp đang hoạt động *"));top.Controls.Add(SupplierBox);top.Controls.Add(SummaryLabel);top.Controls.Add(ConfirmHintLabel);
        var root=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=5,Padding=new Padding(20)};root.RowStyles.Add(new RowStyle(SizeType.AutoSize));root.RowStyles.Add(new RowStyle(SizeType.AutoSize));root.RowStyles.Add(new RowStyle(SizeType.Percent,100));root.RowStyles.Add(new RowStyle(SizeType.AutoSize));root.RowStyles.Add(new RowStyle(SizeType.AutoSize));root.Controls.Add(top,0,0);root.Controls.Add(CatalogUi.Toolbar(AddButton,RemoveButton),0,1);root.Controls.Add(tabs,0,2);root.Controls.Add(MessageLabel,0,3);root.Controls.Add(CatalogUi.Toolbar(SaveButton,ConfirmButton,CancelDraftButton,ReloadButton),0,4);Controls.Add(root);
        SupplierBox.SelectedIndexChanged+=(_,_)=>Actions();LinesGrid.CellValueChanged+=(_,_)=>Actions();LinesGrid.RowsRemoved+=(_,_)=>Actions();
        AddButton.Click+=(_,_)=>{using var picker=new SupplyVariantPicker(api);if(picker.ShowDialog(this)==DialogResult.OK&&picker.Selected is {} choice){if(Lines.Any(l=>l.VariantId==choice.VariantId)){MessageLabel.Text="Biến thể đã có trong phiếu. Hãy sửa lượng trên dòng đó.";return;}Lines.Add(new(){VariantId=choice.VariantId,Sku=choice.Sku,Name=choice.Name,Cost=choice.Cost});Actions();}};
        RemoveButton.Click+=(_,_)=>{if(LinesGrid.CurrentRow?.DataBoundItem is PurchaseLineDraft line){Lines.Remove(line);Actions();}};
        SaveButton.Click+=async(_,_)=>await SaveAsync();ConfirmButton.Click+=async(_,_)=>await ActAsync("confirm");CancelDraftButton.Click+=async(_,_)=>await ActAsync("cancel");ReloadButton.Click+=async(_,_)=>await LoadAsync();Shown+=async(_,_)=>await LoadAsync();Actions();
    }
    private long SupplierId=>(SupplierBox.SelectedItem as SupplierRecord)?.Id??0;
    private PurchaseLineRequest[] Input()=>Lines.Select(l=>l.Request()).ToArray();
    private void Actions()
    {
        if(binding)return;var pending=session.NeedsReadback||session.PendingCreation is not null||operation?.NeedsReadback==true||api.NeedsReconciliation;
        ConfirmHintLabel.Text=SupplyPresentation.Draft(Current)?Current!.Lines.Count==0?"Phiếu rỗng không thể xác nhận. Thêm dòng và lưu nháp trước.":!session.Matches(SupplierId,Input())?"Có thay đổi chưa lưu. Lưu nháp trước khi xác nhận.":"":"";
        var editable=loaded&&!busy&&!pending&&(Current is null||SupplyPresentation.Draft(Current));SaveButton.Enabled=editable;AddButton.Enabled=RemoveButton.Enabled=SupplierBox.Enabled=editable;LinesGrid.ReadOnly=!editable;
        ConfirmButton.Enabled=editable&&Current is{Lines.Count:>0}&&SupplierId==Current.SupplierId&&session.Matches(SupplierId,Input());CancelDraftButton.Enabled=loaded&&!busy&&!pending&&SupplyPresentation.Draft(Current);ReloadButton.Enabled=!busy;
    }
    private void Bind(PurchaseReadback read)
    {
        binding=true;session.Adopt(read);Lines=new(read.Purchase.Lines.Select(l=>new PurchaseLineDraft{VariantId=l.VariantId,Sku=l.Sku,Name=l.Name,Quantity=l.Quantity,Cost=l.Cost}).ToList());LinesGrid.DataSource=Lines;
        SupplierBox.DataSource=suppliers;SupplierBox.SelectedItem=suppliers.SingleOrDefault(s=>s.Id==read.Purchase.SupplierId);
        SummaryLabel.Text=$"{read.Purchase.Code} · {SupplyPresentation.State(read.Purchase.Status)} · {AdminFormatting.Time(read.Purchase.CreatedAt)}\nNgười lập: {(api.Identity?.Id==read.Purchase.CreatorId?api.Identity.Username:$"Tài khoản #{read.Purchase.CreatorId}")} · Tổng tiền: {AdminFormatting.Money(read.Purchase.Total)} · NCC: {read.Purchase.Supplier}";
        ImpactGrid.Rows.Clear();foreach(var row in read.CurrentStock)ImpactGrid.Rows.Add(row.Stock.Sku,row.Cost,row.Price,row.Stock.OnHand,row.Stock.Reserved,row.Stock.Available);binding=false;loaded=true;
    }
    public async Task LoadAsync()
    {
        if(busy)return;if(loaded&&Current is not null&&operation is null&&!session.NeedsReadback&&!api.NeedsReconciliation&&!session.Matches(SupplierId,Input())&&!confirm("Tải lại phiếu","Thay đổi chưa lưu sẽ được bỏ. Tiếp tục?"))return;
        busy=true;Actions();MessageLabel.Text="Đang tải / đối soát phiếu…";
        try
        {
            suppliers=await contacts.ActiveSuppliers();
            if(operation is not null){var read=await workspace.Resolve(operation);Bind(read);MessageLabel.Text=operation.Finished?"Đã đối soát trạng thái phiếu từ máy chủ.":"Phiếu chưa thay đổi. Có thể thử lại cùng thao tác đã xác nhận.";}
            else if(session.Id>0){Bind(await session.Load());MessageLabel.Text=SupplierId==0&&SupplyPresentation.Draft(Current)?"NCC của phiếu đã ngừng. Chọn NCC hoạt động và lưu nháp, hoặc hủy phiếu.":"Đã tải dữ liệu từ máy chủ.";}
            else if(session.PendingCreation is not null){Bind(await session.ResolveCreation());MessageLabel.Text="Đã đối soát tạo phiếu bằng cùng yêu cầu ban đầu.";}
            else{binding=true;SupplierBox.DataSource=suppliers;if(handoff is not null&&!loaded){Lines=new(await workspace.FromProduct(handoff));LinesGrid.DataSource=Lines;}binding=false;loaded=true;MessageLabel.Text="Các dòng mới chưa được lưu. Lưu nháp để có tổng tiền từ máy chủ; có thể lưu nháp rỗng.";}
        }
        catch(Exception error){binding=false;MessageLabel.Text=SupplyPresentation.Failure(error);}finally{busy=false;Actions();}
    }
    public async Task SaveAsync()
    {
        if(busy||!SaveButton.Enabled)return;if(!LinesGrid.EndEdit()){MessageLabel.Text="Hoàn tất ô đang nhập trước khi lưu.";return;}var input=Input();var invalid=SupplyPresentation.LinesError(SupplierId,input);if(invalid is not null){MessageLabel.Text=invalid;return;}
        busy=true;Actions();MessageLabel.Text="Đang lưu nháp…";try{Bind(await session.Save(SupplierId,input));operation=null;MessageLabel.Text="Đã lưu nháp. Kho và giá chưa thay đổi.";}catch(Exception error){MessageLabel.Text=SupplyPresentation.Failure(error);}finally{busy=false;Actions();}
    }
    public async Task ActAsync(string action)
    {
        if(busy||Current is null||!SupplyPresentation.Draft(Current))return;if(!LinesGrid.EndEdit())return;
        if(action=="confirm"&&(Current.Lines.Count==0||!session.Matches(SupplierId,Input()))){MessageLabel.Text=Current.Lines.Count==0?"Phiếu rỗng không thể xác nhận. Thêm dòng và lưu nháp trước.":"Có thay đổi chưa lưu. Lưu nháp trước khi xác nhận.";return;}
        if(session.NeedsReadback||session.PendingCreation is not null||operation?.NeedsReadback==true||api.NeedsReconciliation){MessageLabel.Text="Hãy tải lại / đối soát trước khi gửi tiếp.";return;}
        if(!confirm(action=="confirm"?"Xác nhận nhập hàng":"Hủy phiếu nháp",SupplyPresentation.PurchaseSummary(Current)+(action=="confirm"?"\nXác nhận sẽ ghi tăng tồn và cập nhật giá từ máy chủ.":"\nHủy không đổi tồn hoặc giá. Thay đổi chưa lưu sẽ bỏ.")))return;
        if(operation is null||operation.Finished||operation.Action!=action)operation=new(Current,action);
        busy=true;Actions();MessageLabel.Text="Đang xử lý…";
        try{var read=await workspace.Execute(operation);Bind(read);MessageLabel.Text=action=="confirm"?"Đã xác nhận. Tồn và giá hiện tại đã tải từ máy chủ.":"Đã hủy phiếu. Kho và giá giữ nguyên.";}
        catch(Exception error){MessageLabel.Text=SupplyPresentation.Failure(error)+(operation?.NeedsReadback==true?" Chọn đối soát để xác định kết quả mới.":"");}finally{busy=false;Actions();}
    }
}
