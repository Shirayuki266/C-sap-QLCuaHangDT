using PhoneStore.Admin.Client;

namespace PhoneStore.Admin;

public sealed class ProductListForm : Form
{
    private readonly IAdminApi api;
    private readonly CatalogWorkspace workspace;
    private readonly Func<string,string,bool> confirm;
    private List<LookupRow> brands=[],categories=[];
    private int page=1,total;
    private bool busy;
    public TextBox QueryBox { get; }=AdminTheme.Input("Tìm mã, tên hoặc SKU");
    public ComboBox BrandBox { get; }=new(){DropDownStyle=ComboBoxStyle.DropDownList,Width=200,AccessibleName="Lọc hãng"};
    public ComboBox StateBox { get; }=new(){DropDownStyle=ComboBoxStyle.DropDownList,Width=180,AccessibleName="Lọc trạng thái"};
    public Button SearchButton { get; }=AdminTheme.Button("Tìm kiếm");
    public Button NewButton { get; }=AdminTheme.Button("Thêm điện thoại");
    public Button DetailsButton { get; }=AdminTheme.Button("Chi tiết",false);
    public Button EditButton { get; }=AdminTheme.Button("Sửa",false);
    public Button StateButton { get; }=AdminTheme.Button("Ngừng / mở kinh doanh",false);
    public Button PreviousButton { get; }=AdminTheme.Button("Trang trước",false);
    public Button NextButton { get; }=AdminTheme.Button("Trang sau",false);
    public DataGridView Grid { get; }=CatalogUi.Grid();
    public Label MessageLabel { get; }=AdminTheme.Label("");
    public ProductListForm(IAdminApi api,Func<string,string,bool>? confirmation=null)
    {
        this.api=api;workspace=new(api);confirm=confirmation??((t,m)=>CatalogUi.Confirm(this,t,m));AdminTheme.Frame(this,"Điện thoại");
        QueryBox.Dock=DockStyle.None;QueryBox.Width=220;QueryBox.PlaceholderText="Tìm mã, tên hoặc SKU";StateBox.Items.AddRange(["Tất cả trạng thái","Đang kinh doanh","Đã ngừng"]);StateBox.SelectedIndex=0;
        Grid.ReadOnly=true;foreach(var (name,title) in new[]{("Code","Mã"),("Name","Điện thoại"),("Brand","Hãng"),("Category","Danh mục"),("State","Trạng thái"),("Variants","Biến thể")})CatalogUi.Column(Grid,name,title,true,name=="Name"?240:100);
        var footer=CatalogUi.Toolbar(PreviousButton,NextButton,MessageLabel);
        CatalogUi.Layout(this,"Điện thoại",CatalogUi.Toolbar(QueryBox,BrandBox,StateBox,SearchButton,NewButton,DetailsButton,EditButton,StateButton),Grid,footer);
        SearchButton.Click+=async(_,_)=>{page=1;await LoadAsync();};PreviousButton.Click+=async(_,_)=>{if(page>1){page--;await LoadAsync();}};NextButton.Click+=async(_,_)=>{if(page*20<total){page++;await LoadAsync();}};
        NewButton.Click+=async(_,_)=>{using var editor=new ProductEditor(api);editor.ShowDialog(this);await LoadAsync();};
        EditButton.Click+=async(_,_)=>{if(Selected is {} p){using var editor=new ProductEditor(api,p.Id);editor.ShowDialog(this);await LoadAsync();}};
        DetailsButton.Click+=(_,_)=>{if(Selected is {} p){using var detail=new ProductDetailsForm(api,p.Id);detail.ShowDialog(this);}};
        StateButton.Click+=async(_,_)=>await ChangeStateAsync();Grid.SelectionChanged+=(_,_)=>UpdateActions();Shown+=async(_,_)=>await LoadAsync();AcceptButton=SearchButton;
    }
    public ProductRecord? Selected=>Grid.CurrentRow?.Tag as ProductRecord;
    public async Task LoadAsync()
    {
        if(busy)return;SetBusy(true);
        try
        {
            var selectedBrand=(BrandBox.SelectedItem as LookupRow)?.Id??0;
            brands=await workspace.Lookups("brands");categories=await workspace.Lookups("categories");BrandBox.DataSource=new[]{new LookupRow(0,"Tất cả hãng",true)}.Concat(brands).ToList();BrandBox.SelectedItem=((List<LookupRow>)BrandBox.DataSource).SingleOrDefault(b=>b.Id==selectedBrand);
            if(workspace.PendingProductId is {} pending)await workspace.Product(pending);
            PageResult<ProductRecord> batch=null!;
            if(api.NeedsReconciliation)await api.ReconcileAsync(async ct=>batch=await workspace.Products(QueryBox.Text,selectedBrand>0?selectedBrand:null,StateBox.SelectedIndex==0?null:StateBox.SelectedIndex==1,page,ct:ct));
            else batch=await workspace.Products(QueryBox.Text,selectedBrand>0?selectedBrand:null,StateBox.SelectedIndex==0?null:StateBox.SelectedIndex==1,page);
            total=batch.Total;Grid.Rows.Clear();foreach(var p in batch.Items){var i=Grid.Rows.Add(p.Code,p.Name,brands.SingleOrDefault(b=>b.Id==p.BrandId)?.Name??$"#{p.BrandId}",categories.SingleOrDefault(c=>c.Id==p.CategoryId)?.Name??$"#{p.CategoryId}",p.Active?"Đang kinh doanh":"Đã ngừng",p.Variants.Count);Grid.Rows[i].Tag=p;}
            MessageLabel.Text=total==0?"Không có điện thoại phù hợp. Thử đổi từ khóa hoặc bộ lọc.":$"Trang {page} · {total} điện thoại · 20 dòng/trang";
        }
        catch(Exception error){Grid.Rows.Clear();MessageLabel.Text=AdminTheme.Failure(error);}finally{SetBusy(false);}
    }
    public async Task ChangeStateAsync()
    {
        if(busy||Selected is not {} selected)return;SetBusy(true);
        try
        {
            var current=await workspace.Product(selected.Id);
            if(!confirm(current.Active?"Ngừng kinh doanh":"Mở kinh doanh",$"Xác nhận đổi trạng thái “{current.Name}”? Không xóa sản phẩm hoặc lịch sử."))return;
            await workspace.Save(current.Id,CatalogWorkspace.Request(current) with {Active=!current.Active});
        }
        catch(Exception error){MessageLabel.Text=AdminTheme.Failure(error);return;}
        finally{SetBusy(false);}
        await LoadAsync();
    }
    private void UpdateActions(){DetailsButton.Enabled=EditButton.Enabled=StateButton.Enabled=!busy&&Selected is not null;}
    private void SetBusy(bool value){busy=value;SearchButton.Enabled=NewButton.Enabled=Grid.Enabled=QueryBox.Enabled=BrandBox.Enabled=StateBox.Enabled=!value;PreviousButton.Enabled=!value&&page>1;NextButton.Enabled=!value&&page*20<total;UpdateActions();if(value)MessageLabel.Text="Đang tải…";}
}

public sealed class ProductDetailsForm : Form
{
    private readonly CatalogWorkspace workspace;
    private readonly long id;
    public Label MessageLabel { get; }=AdminTheme.Label("");
    public TextBox DetailsBox { get; }=new(){Multiline=true,ReadOnly=true,Dock=DockStyle.Fill,ScrollBars=ScrollBars.Both,AccessibleName="Thông tin sản phẩm và ảnh"};
    public DataGridView Grid { get; }=CatalogUi.Grid();
    public ProductDetailsForm(IAdminApi api,long id)
    {
        workspace=new(api);this.id=id;AdminTheme.Frame(this,"Chi tiết điện thoại");Grid.ReadOnly=true;
        foreach(var (property,title) in new[]{("Sku","SKU"),("Color","Màu"),("StorageGb","GB"),("Cost","Giá vốn (đ)"),("ProfitPercent","Lợi nhuận %"),("Price","Giá bán (đ)"),("OnHand","Tồn thực"),("Reserved","Đang giữ"),("Available","Có thể bán"),("Version","Version")})CatalogUi.Column(Grid,property,title,true);
        Grid.Columns["Available"]!.MinimumWidth=150;
        var split=new SplitContainer{Dock=DockStyle.Fill,Orientation=Orientation.Horizontal,SplitterDistance=190};split.Panel1.Controls.Add(DetailsBox);split.Panel2.Controls.Add(Grid);
        var root=new TableLayoutPanel{Dock=DockStyle.Fill,RowCount=3,ColumnCount=1,Padding=new Padding(20)};root.RowStyles.Add(new RowStyle(SizeType.AutoSize));root.RowStyles.Add(new RowStyle(SizeType.Percent,100));root.RowStyles.Add(new RowStyle(SizeType.AutoSize));root.Controls.Add(AdminTheme.Label("Chi tiết điện thoại",22,true),0,0);root.Controls.Add(split,0,1);root.Controls.Add(MessageLabel,0,2);Controls.Add(root);Shown+=async(_,_)=>await LoadAsync();
    }
    public async Task LoadAsync()
    {
        MessageLabel.Text="Đang tải…";
        try{var p=await workspace.Product(id);DetailsBox.Text=$"{p.Code} · {p.Name}\r\n{(p.Active?"Đang kinh doanh":"Đã ngừng")}\r\n{p.Specifications}\r\n{p.Description}\r\nẢnh chính: {p.ImageUrl}\r\n"+string.Join("\r\n",p.Images.OrderBy(i=>i.SortOrder).Select(i=>i.Url));Grid.DataSource=p.Variants;MessageLabel.Text="Giá và số dư từ máy chủ. Có thể bán = tồn thực − đang giữ.";}
        catch(Exception error){MessageLabel.Text=AdminTheme.Failure(error);}
    }
}
