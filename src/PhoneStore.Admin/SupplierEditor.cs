using PhoneStore.Admin.Client;

namespace PhoneStore.Admin;

public sealed class SupplierEditor:Form
{
    private readonly IAdminApi api;
    private readonly ContactsWorkspace workspace;
    private readonly Func<string,string,bool> confirm;
    private SupplierRecord? original;
    private bool busy,loaded;
    public SupplierRecord? Saved { get; private set; }
    public TextBox CodeBox { get; }=AdminTheme.Input("Mã nhà cung cấp");
    public TextBox NameBox { get; }=AdminTheme.Input("Tên nhà cung cấp");
    public TextBox PhoneBox { get; }=AdminTheme.Input("Điện thoại");
    public TextBox AddressBox { get; }=AdminTheme.Input("Địa chỉ");
    public CheckBox ActiveBox { get; }=new(){Text="Đang hoạt động",Checked=true,AutoSize=true};
    public Button SaveButton { get; }=AdminTheme.Button("Lưu NCC");
    public Button ReloadButton { get; }=AdminTheme.Button("Tải lại / đối soát",false);
    public Label MessageLabel { get; }=AdminTheme.Label("");
    public SupplierEditor(IAdminApi api,SupplierRecord? supplier=null,Func<string,string,bool>? confirmation=null)
    {
        this.api=api;workspace=new(api);original=supplier;confirm=confirmation??((t,m)=>CatalogUi.Confirm(this,t,m));AdminTheme.Frame(this,supplier is null?"Thêm nhà cung cấp":"Sửa nhà cung cấp");
        CodeBox.MaxLength=30;NameBox.MaxLength=200;PhoneBox.MaxLength=20;AddressBox.MaxLength=500;AddressBox.Multiline=true;AddressBox.Height=100;
        var body=AdminTheme.Stack(24);body.Dock=DockStyle.Top;AdminTheme.Add(body,AdminTheme.Label(supplier is null?"Thêm nhà cung cấp":"Sửa nhà cung cấp",22,true));
        AdminTheme.Field(body,"Mã NCC *",CodeBox);AdminTheme.Field(body,"Tên nhà cung cấp *",NameBox);AdminTheme.Field(body,"Điện thoại *",PhoneBox);AdminTheme.Field(body,"Địa chỉ *",AddressBox);AdminTheme.Add(body,ActiveBox);AdminTheme.Add(body,MessageLabel);AdminTheme.Add(body,CatalogUi.Toolbar(SaveButton,ReloadButton));Controls.Add(body);
        SaveButton.Enabled=supplier is null;SaveButton.Click+=async(_,_)=>await SaveAsync();ReloadButton.Click+=async(_,_)=>await LoadAsync();Shown+=async(_,_)=>await LoadAsync();
    }
    public SupplierRequest Input()=>new(CodeBox.Text,NameBox.Text,PhoneBox.Text,AddressBox.Text,ActiveBox.Checked);
    private void Bind(SupplierRecord row){original=Saved=row;loaded=true;CodeBox.Text=row.Code;NameBox.Text=row.Name;PhoneBox.Text=row.Phone;AddressBox.Text=row.Address;ActiveBox.Checked=row.Active;}
    public async Task LoadAsync()
    {
        if(busy)return;if(loaded&&!api.NeedsReconciliation&&!workspace.PendingSupplier.HasValue&&!confirm("Tải lại NCC","Thay các thay đổi chưa lưu bằng dữ liệu máy chủ?"))return;SetBusy(true);
        try
        {
            var id=workspace.PendingSupplier?.Id??original?.Id??0;var code=workspace.PendingSupplier?.Code??original?.Code??CodeBox.Text;
            SupplierRecord? row=null;
            if(api.NeedsReconciliation)
            {
                if(id==0&&string.IsNullOrWhiteSpace(code))throw new AdminApiException("UNCERTAIN");
                row=await workspace.ReconcileSupplier(id,code);
            }
            else if(id>0)row=await workspace.FindSupplier(id,code);
            if(row is not null){Bind(row);MessageLabel.Text="Đã tải lại NCC từ danh sách máy chủ.";}
            else if(id>0)throw new AdminApiException("NOT_FOUND",404);
            else MessageLabel.Text="Nhập thông tin NCC. Chỉ NCC hoạt động có trong danh sách chọn lập phiếu mới.";
        }
        catch(Exception error){MessageLabel.Text=AdminTheme.Failure(error);}finally{SetBusy(false);}
    }
    public async Task SaveAsync()
    {
        if(busy||original is not null&&!loaded)return;var request=Input();var invalid=ContactForms.SupplierError(request);if(invalid is not null){MessageLabel.Text=invalid;return;}
        if(original is not null&&request.Active!=original.Active&&!confirm(request.Active?"Mở NCC":"Ngừng NCC","NCC ngừng hoạt động không có trong danh sách chọn khi lập phiếu mới. Dữ liệu và lịch sử được giữ nguyên. Tiếp tục?"))return;
        SetBusy(true);try{Bind(await workspace.SaveSupplier(original?.Id??0,request));MessageLabel.Text="Đã lưu và làm mới từ danh sách máy chủ theo mã NCC.";}
        catch(Exception error){MessageLabel.Text=AdminTheme.Failure(error);}finally{SetBusy(false);}
    }
    private void SetBusy(bool value){busy=value;SaveButton.Enabled=!value&&(original is null||loaded);ReloadButton.Enabled=CodeBox.Enabled=NameBox.Enabled=PhoneBox.Enabled=AddressBox.Enabled=ActiveBox.Enabled=!value;if(value)MessageLabel.Text="Đang xử lý…";}
}
