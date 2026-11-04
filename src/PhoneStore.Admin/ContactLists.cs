using PhoneStore.Admin.Client;

namespace PhoneStore.Admin;

public abstract class PagedContactsForm:Form
{
    protected bool Busy;
    protected int Page=1,Total;
    public TextBox QueryBox { get; }=AdminTheme.Input("Tìm tên, điện thoại hoặc mã");
    public ComboBox StateBox { get; }=new(){DropDownStyle=ComboBoxStyle.DropDownList,Width=180,AccessibleName="Lọc trạng thái"};
    public Button SearchButton { get; }=AdminTheme.Button("Tìm kiếm");
    public Button PreviousButton { get; }=AdminTheme.Button("Trang trước",false);
    public Button NextButton { get; }=AdminTheme.Button("Trang sau",false);
    public Label MessageLabel { get; }=AdminTheme.Label("");
    public DataGridView Grid { get; }=CatalogUi.Grid();
    protected readonly FlowLayoutPanel Toolbar;
    protected bool? ActiveFilter=>StateBox.SelectedIndex==0?null:StateBox.SelectedIndex==1;
    protected PagedContactsForm(string title)
    {
        AdminTheme.Frame(this,title);QueryBox.Dock=DockStyle.None;QueryBox.Width=280;QueryBox.MaxLength=100;QueryBox.PlaceholderText="Tìm tên, điện thoại hoặc mã";
        StateBox.Items.AddRange(["Tất cả trạng thái","Đang hoạt động","Đã khóa / ngừng"]);StateBox.SelectedIndex=0;Grid.ReadOnly=true;
        Toolbar=CatalogUi.Toolbar(QueryBox,StateBox,SearchButton);CatalogUi.Layout(this,title,Toolbar,Grid,CatalogUi.Toolbar(PreviousButton,NextButton,MessageLabel));
        SearchButton.Click+=async(_,_)=>{Page=1;await LoadAsync();};PreviousButton.Click+=async(_,_)=>{if(Page>1){Page--;await LoadAsync();}};NextButton.Click+=async(_,_)=>{if(Page*20<Total){Page++;await LoadAsync();}};
        Grid.SelectionChanged+=(_,_)=>Actions();Shown+=async(_,_)=>await LoadAsync();AcceptButton=SearchButton;
    }
    protected abstract Task ReadRows();
    protected virtual void Actions(){}
    public async Task LoadAsync()
    {
        if(Busy)return;Busy=true;SearchButton.Enabled=QueryBox.Enabled=StateBox.Enabled=Grid.Enabled=false;PreviousButton.Enabled=NextButton.Enabled=false;Actions();MessageLabel.Text="Đang tải…";
        try{await ReadRows();MessageLabel.Text=Total==0?"Không có dữ liệu phù hợp. Thử đổi từ khóa hoặc trạng thái.":$"Trang {Page} · {Total} mục · 20 dòng/trang";}
        catch(Exception error){Grid.Rows.Clear();MessageLabel.Text=AdminTheme.Failure(error);}
        finally{Busy=false;SearchButton.Enabled=QueryBox.Enabled=StateBox.Enabled=Grid.Enabled=true;PreviousButton.Enabled=Page>1;NextButton.Enabled=Page*20<Total;Actions();}
    }
}

public sealed class CustomerListForm:PagedContactsForm
{
    private readonly IAdminApi api;
    private readonly ContactsWorkspace workspace;
    public Button DetailsButton { get; }=AdminTheme.Button("Chi tiết khách",false);
    public CustomerListForm(IAdminApi api):base("Khách hàng")
    {
        this.api=api;workspace=new(api);QueryBox.PlaceholderText="Tên, điện thoại hoặc email";QueryBox.AccessibleName="Tìm khách theo tên, điện thoại, email";Toolbar.Controls.Add(DetailsButton);
        foreach(var (property,title) in new[]{("Username","Tài khoản"),("FullName","Họ tên"),("Phone","Điện thoại"),("Email","Email"),("State","Trạng thái")})CatalogUi.Column(Grid,property,title,true,property=="FullName"?240:150);
        DetailsButton.Click+=async(_,_)=>{if(Grid.CurrentRow?.Tag is CustomerRecord row){using var form=new CustomerDetailsForm(api,row.Id);form.ShowDialog(this);await LoadAsync();}};
    }
    protected override async Task ReadRows()
    {
        var batch=await workspace.Customers(QueryBox.Text,ActiveFilter,Page);Total=batch.Total;Grid.Rows.Clear();
        foreach(var row in batch.Items){var index=Grid.Rows.Add(row.Username,row.FullName,row.Phone,row.Email??"",row.Active?"Đang hoạt động":"Đã khóa");Grid.Rows[index].Tag=row;}
    }
    protected override void Actions(){if(DetailsButton is not null)DetailsButton.Enabled=!Busy&&Grid.CurrentRow?.Tag is CustomerRecord;}
}

public sealed class SupplierListForm:PagedContactsForm
{
    private readonly IAdminApi api;
    private readonly ContactsWorkspace workspace;
    public Button NewButton { get; }=AdminTheme.Button("Thêm NCC");
    public Button EditButton { get; }=AdminTheme.Button("Sửa NCC",false);
    public Button PickerButton { get; }=AdminTheme.Button("Chọn NCC hoạt động",false);
    public SupplierListForm(IAdminApi api):base("Nhà cung cấp")
    {
        this.api=api;workspace=new(api);Toolbar.Controls.AddRange([NewButton,EditButton,PickerButton]);
        foreach(var (property,title) in new[]{("Code","Mã NCC"),("Name","Nhà cung cấp"),("Phone","Điện thoại"),("Address","Địa chỉ"),("State","Trạng thái")})CatalogUi.Column(Grid,property,title,true,property is "Name" or "Address"?240:150);
        NewButton.Click+=async(_,_)=>{using var form=new SupplierEditor(api);form.ShowDialog(this);await LoadAsync();};
        EditButton.Click+=async(_,_)=>{if(Grid.CurrentRow?.Tag is SupplierRecord row){using var form=new SupplierEditor(api,row);form.ShowDialog(this);await LoadAsync();}};
        PickerButton.Click+=(_,_)=>{using var form=new SupplierPickerForm(api);form.ShowDialog(this);};
    }
    protected override async Task ReadRows()
    {
        var batch=await workspace.Suppliers(QueryBox.Text,ActiveFilter,Page);Total=batch.Total;Grid.Rows.Clear();
        foreach(var row in batch.Items){var index=Grid.Rows.Add(row.Code,row.Name,row.Phone,row.Address,row.Active?"Đang hoạt động":"Đã ngừng");Grid.Rows[index].Tag=row;}
    }
    protected override void Actions(){if(NewButton is not null)NewButton.Enabled=PickerButton.Enabled=!Busy;if(EditButton is not null)EditButton.Enabled=!Busy&&Grid.CurrentRow?.Tag is SupplierRecord;}
}

public sealed class SupplierPickerForm:Form
{
    private readonly ContactsWorkspace workspace;
    public ListBox SuppliersBox { get; }=new(){Dock=DockStyle.Fill,AccessibleName="Nhà cung cấp đang hoạt động"};
    public Button ChooseButton { get; }=AdminTheme.Button("Chọn NCC");
    public Label MessageLabel { get; }=AdminTheme.Label("");
    public SupplierRecord? Selected { get; private set; }
    public SupplierPickerForm(IAdminApi api)
    {
        workspace=new(api);AdminTheme.Frame(this,"Chọn nhà cung cấp");var root=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=4,Padding=new Padding(24)};
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));root.RowStyles.Add(new RowStyle(SizeType.Percent,100));root.RowStyles.Add(new RowStyle(SizeType.AutoSize));root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.Controls.Add(AdminTheme.Label("Nhà cung cấp đang hoạt động",22,true),0,0);root.Controls.Add(SuppliersBox,0,1);root.Controls.Add(MessageLabel,0,2);root.Controls.Add(ChooseButton,0,3);Controls.Add(root);ChooseButton.Enabled=false;
        ChooseButton.Click+=(_,_)=>{if(SuppliersBox.SelectedItem is SupplierRecord{Active:true} row){Selected=row;DialogResult=DialogResult.OK;Close();}};Shown+=async(_,_)=>await LoadAsync();
    }
    public async Task LoadAsync()
    {
        ChooseButton.Enabled=false;MessageLabel.Text="Đang tải NCC hoạt động…";
        try{var rows=await workspace.ActiveSuppliers();SuppliersBox.DataSource=rows;ChooseButton.Enabled=rows.Count>0;MessageLabel.Text=rows.Count==0?"Chưa có NCC hoạt động. Hãy thêm hoặc mở lại NCC trước khi lập phiếu.":"Chỉ gồm NCC hoạt động. Chọn nhà cung cấp chưa tạo hoặc lưu phiếu nhập.";}
        catch(Exception error){SuppliersBox.DataSource=null;MessageLabel.Text=AdminTheme.Failure(error);}
    }
}
