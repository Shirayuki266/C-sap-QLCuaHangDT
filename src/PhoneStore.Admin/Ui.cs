using System.Reflection;
using PhoneStore.Core.Contracts;
namespace PhoneStore.Admin;
public static class Ui
{
    private static readonly Dictionary<string,string> labels=new(){["Id"]="ID",["VariantId"]="ID biến thể",["Code"]="Mã",["Name"]="Tên",["Username"]="Tên đăng nhập",["FullName"]="Họ tên",["Phone"]="Điện thoại",["Email"]="Email",["Address"]="Địa chỉ",["Active"]="Hoạt động",["Brand"]="Hãng",["Category"]="Danh mục",["Sku"]="SKU",["Color"]="Màu",["StorageGb"]="Dung lượng GB",["Price"]="Giá bán",["Cost"]="Giá vốn",["OnHand"]="Tồn thực tế",["Reserved"]="Đang giữ",["Available"]="Có thể bán",["Total"]="Tổng tiền",["Quantity"]="Số lượng",["Status"]="Trạng thái",["Payment"]="Thanh toán COD",["Customer"]="Khách hàng",["Recipient"]="Người nhận",["CreatedAt"]="Ngày lập",["CompletedAt"]="Hoàn tất",["ConfirmedAt"]="Xác nhận lúc",["Supplier"]="Nhà cung cấp",["Creator"]="Người lập",["Confirmer"]="Người xác nhận",["At"]="Thời điểm",["Kind"]="Loại",["DeltaOnHand"]="Chênh lệch tồn",["DeltaReserved"]="Chênh lệch giữ",["Actor"]="Người thực hiện",["Document"]="Chứng từ",["Reason"]="Lý do",["Label"]="Nhãn",["Count"]="Số lượng / số đơn",["Value"]="Giá trị",["Specifications"]="Cấu hình",["Description"]="Mô tả",["ImageUrl"]="URL ảnh",["ReturnReceived"]="Đã nhận lại",["From"]="Từ trạng thái",["To"]="Đến trạng thái",["Variant"]="Biến thể"};
    public static string Label(string key)=>labels.GetValueOrDefault(key,key);
    public static DataGridView Grid()=>new(){Dock=DockStyle.Fill,ReadOnly=true,AutoGenerateColumns=true,AllowUserToAddRows=false,AllowUserToDeleteRows=false,SelectionMode=DataGridViewSelectionMode.FullRowSelect,MultiSelect=false,AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.DisplayedCells,RowHeadersVisible=false,BackgroundColor=Color.White,BorderStyle=BorderStyle.FixedSingle};
    public static void Format(DataGridView grid) {
        foreach(DataGridViewColumn column in grid.Columns) {column.HeaderText=Label(column.Name);if(column.Name is "Version" or "Variants" or "Lines" or "History" or "CustomerId" or "BrandId" or "CategoryId" or "SupplierId" or "ImageUrl")column.Visible=false; if(column.ValueType==typeof(decimal))column.DefaultCellStyle.Format="N0";}
        if(grid.Tag is true)return;MoneyCells(grid);grid.Tag=true;grid.CellFormatting+=(_,e)=>{if(e.Value is string s&&grid.Columns[e.ColumnIndex].Name is "Status" or "Payment" or "From" or "To" or "Kind")e.Value=grid.Columns[e.ColumnIndex].Name=="Kind"&&s=="NHAP"?"Nhập kho":Labels.Status(s);if(e.Value is DateTime dt)e.Value=Labels.Local(dt).ToString("dd/MM/yyyy HH:mm");};
    }
    public static void MoneyCells(DataGridView grid) => grid.CellFormatting+=(_,e)=>{if(e.Value is decimal value){e.Value=value.FormatMoney();e.FormattingApplied=true;}};
    public static bool Confirm(string message)=>MessageBox.Show(message,"Xác nhận thao tác",MessageBoxButtons.YesNo,MessageBoxIcon.Question)==DialogResult.Yes;
}
public sealed class ListScreen<T>:UserControl where T:class
{
    private readonly DataGridView grid=Ui.Grid();private readonly Label status=new(){AutoSize=true,Padding=new(8)};
    public readonly FlowLayoutPanel Tools=new(){Dock=DockStyle.Top,AutoSize=true,Padding=new(8),WrapContents=true};
    public readonly TextBox Search=new(){Width=240,PlaceholderText="Tìm kiếm…"};
    public readonly ComboBox Filter=new(){Width=180,DropDownStyle=ComboBoxStyle.DropDownList};
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)] public Func<int,string,Task<Page<T>>>? Loader {get;set;}
    public T? Selected=>grid.CurrentRow?.DataBoundItem as T;
    public int PageNumber {get;private set;}=1;
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)] public string? LastError {get;private set;}
    private bool busy;
    public ListScreen() {
        Dock=DockStyle.Fill;var top=new FlowLayoutPanel{Dock=DockStyle.Top,AutoSize=true,Padding=new(8)};top.Controls.Add(Search);Controls.Add(grid);Controls.Add(Tools);Controls.Add(top);
        var bottom=new FlowLayoutPanel{Dock=DockStyle.Bottom,AutoSize=true};Controls.Add(bottom);var prev=new Button{Text="← Trước",AutoSize=true};var next=new Button{Text="Sau →",AutoSize=true};bottom.Controls.AddRange([prev,next,status]);
        var find=new Button{Text="Tìm / Tải lại",AutoSize=true};top.Controls.Add(find);top.Controls.Add(Filter);Filter.Visible=false;
        find.Click+=async(_,_)=>{PageNumber=1;await RefreshData();};prev.Click+=async(_,_)=>{if(PageNumber>1){PageNumber--;await RefreshData();}};next.Click+=async(_,_)=>{PageNumber++;await RefreshData();};
        Search.KeyDown+=async(_,e)=>{if(e.KeyCode==Keys.Enter){e.SuppressKeyPress=true;PageNumber=1;await RefreshData();}};
    }
    public void AddAction(string title,Func<T?,Task> action,bool selected=true) {
        var button=new Button{Text=title,AutoSize=true,Padding=new(8,4,8,4)};Tools.Controls.Add(button);button.Click+=async(_,_)=>{if(busy)return;if(selected&&Selected is null){status.Text="Vui lòng chọn một dòng.";return;}await Run(async()=>{await action(Selected);await LoadRows();});};
    }
    public async Task Run(Func<Task> action) {if(busy)return;busy=true;LastError=null;Tools.Enabled=false;UseWaitCursor=true;status.Text="Đang xử lý…";try{await action();}catch(Exception e){LastError=e.Message;status.Text="Lỗi: "+e.Message;if(FindForm()?.Opacity!=0)MessageBox.Show(e.Message,"Thông báo",MessageBoxButtons.OK,MessageBoxIcon.Warning);}finally{UseWaitCursor=false;Tools.Enabled=true;busy=false;}}
    private async Task LoadRows() {
        if(Loader==null)return;var data=await Loader(PageNumber,Search.Text);if(data.Items.Count==0&&PageNumber>1){PageNumber--;data=await Loader(PageNumber,Search.Text);}grid.DataSource=data.Items;Ui.Format(grid);status.Text=data.Total==0?"Không có dữ liệu phù hợp.":$"Trang {PageNumber} · {data.Total} bản ghi · Đã tải thành công";
    }
    public Task RefreshData()=>Run(LoadRows);
}
public sealed record Field(string Key,string Label,object? Value,bool Password=false,Dictionary<string,string>? Options=null,bool Multiline=false);
public sealed class EditDialog:Form
{
    private readonly Dictionary<string,Control> inputs=[];
    private readonly Label error=new(){AutoSize=true,ForeColor=Color.Firebrick,MaximumSize=new(620,0)};
    public EditDialog(string title,IEnumerable<Field> fields,Func<EditDialog,Task> save) {
        Text=title;Size=new(720,650);MinimumSize=new(550,450);StartPosition=FormStartPosition.CenterParent;Font=new("Segoe UI",10);
        var panel=new TableLayoutPanel{Dock=DockStyle.Fill,AutoScroll=true,ColumnCount=1,Padding=new(20)};Controls.Add(panel);
        foreach(var field in fields) {panel.Controls.Add(new Label{Text=field.Label,AutoSize=true});Control input;
            if(field.Options!=null){var combo=new ComboBox{DropDownStyle=ComboBoxStyle.DropDownList,Dock=DockStyle.Top,DisplayMember="Value",ValueMember="Key",DataSource=field.Options.ToList()};combo.SelectedValue=field.Value?.ToString()??"";input=combo;}
            else if(field.Value is bool flag)input=new CheckBox{Checked=flag,Text="Có",AutoSize=true};
            else input=new TextBox{Text=field.Value?.ToString()??"",Dock=DockStyle.Top,UseSystemPasswordChar=field.Password,Multiline=field.Multiline,Height=field.Multiline?85:28,MaxLength=field.Password?128:50000};
            inputs[field.Key]=input;panel.Controls.Add(input);if(field.Options!=null)Shown+=(_,_)=>((ComboBox)input).SelectedValue=field.Value?.ToString()??"";
        }
        var button=new Button{Text="Lưu",AutoSize=true,Padding=new(12,6,12,6)};panel.Controls.Add(button);panel.Controls.Add(error);
        button.Click+=async(_,_)=>{button.Enabled=false;error.Text="Đang lưu…";try{await save(this);DialogResult=DialogResult.OK;Close();}catch(Exception e){error.Text=e.Message;}finally{button.Enabled=true;}};
    }
    public string Value(string key)=>inputs[key] is ComboBox combo?combo.SelectedValue?.ToString()??"":inputs[key].Text;
    public bool Flag(string key)=>((CheckBox)inputs[key]).Checked;
    public int Int(string key)=>int.TryParse(Value(key),out var value)?value:throw new InvalidOperationException("Giá trị số không hợp lệ.");
    public long Long(string key)=>long.TryParse(Value(key),out var value)?value:throw new InvalidOperationException("Giá trị số không hợp lệ.");
}








