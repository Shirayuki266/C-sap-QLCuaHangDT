using PhoneStore.Admin.Client;

namespace PhoneStore.Admin;

public sealed class LookupForm : Form
{
    private readonly CatalogWorkspace workspace;
    private readonly IAdminApi api;
    private readonly string kind;
    private readonly Func<string,string,bool> confirm;
    private LookupRow? selected;
    private bool busy;
    public TextBox NameBox { get; }=AdminTheme.Input("Tên hãng hoặc danh mục");
    public CheckBox ActiveBox { get; }=new(){Text="Đang kinh doanh",Checked=true,AutoSize=true};
    public Button SaveButton { get; }=AdminTheme.Button("Lưu");
    public Button NewButton { get; }=AdminTheme.Button("Tạo mới",false);
    public Button ReloadButton { get; }=AdminTheme.Button("Tải lại / đối soát",false);
    public Label MessageLabel { get; }=AdminTheme.Label("");
    public DataGridView Grid { get; }=CatalogUi.Grid();
    public LookupForm(IAdminApi api,string kind,Func<string,string,bool>? confirmation=null)
    {
        this.api=api;workspace=new(api);this.kind=CatalogWorkspace.Kind(kind);confirm=confirmation??((t,m)=>CatalogUi.Confirm(this,t,m));
        AdminTheme.Frame(this,kind=="brands"?"Hãng điện thoại":"Danh mục");NameBox.Width=360;NameBox.Dock=DockStyle.None;
        Grid.ReadOnly=true;CatalogUi.Column(Grid,"Id","Mã",true);CatalogUi.Column(Grid,"Name","Tên",true,250);CatalogUi.Column(Grid,"State","Trạng thái",true,150);
        CatalogUi.Layout(this,kind=="brands"?"Hãng điện thoại":"Danh mục",CatalogUi.Toolbar(NewButton,AdminTheme.Label("Tên *"),NameBox,ActiveBox,SaveButton,ReloadButton),Grid,MessageLabel);
        Grid.SelectionChanged+=(_,_)=>SelectCurrent();
        NewButton.Click+=(_,_)=>{Grid.CurrentCell=null;Grid.ClearSelection();selected=null;NameBox.Clear();ActiveBox.Checked=true;NameBox.Focus();};
        ReloadButton.Click+=async(_,_)=>await ReloadAsync();SaveButton.Click+=async(_,_)=>await SaveAsync();Shown+=async(_,_)=>await ReloadAsync();
    }
    private void Display(List<LookupRow> rows)
    {selected=null;Grid.Rows.Clear();foreach(var row in rows){var index=Grid.Rows.Add(row.Id,row.Name,row.Active?"Đang kinh doanh":"Đã ngừng");Grid.Rows[index].Tag=row;}SelectCurrent();MessageLabel.Text=rows.Count==0?"Chưa có dữ liệu. Chọn Tạo mới để thêm.":$"{rows.Count} mục · Gồm cả mục đã ngừng";}
    private void SelectCurrent(){if(Grid.CurrentRow?.Tag is LookupRow row){selected=row;NameBox.Text=row.Name;ActiveBox.Checked=row.Active;}}
    public async Task ReloadAsync()
    {
        if(busy)return;SetBusy(true);
        try{List<LookupRow> rows=[];if(api.NeedsReconciliation)await api.ReconcileAsync(async ct=>rows=await workspace.Lookups(kind,ct));else rows=await workspace.Lookups(kind);Display(rows);}
        catch(Exception error){MessageLabel.Text=AdminTheme.Failure(error);}finally{SetBusy(false);}
    }
    public async Task SaveAsync()
    {
        if(busy)return;
        if(string.IsNullOrWhiteSpace(NameBox.Text)||NameBox.Text.Trim().Length>100){MessageLabel.Text="Nhập tên từ 1 đến 100 ký tự.";return;}
        if(selected is not null&&selected.Active!=ActiveBox.Checked&&!confirm(ActiveBox.Checked?"Mở kinh doanh":"Ngừng kinh doanh","Khi ngừng hãng/danh mục, sản phẩm liên quan không còn hiển thị ở website. Dữ liệu và lịch sử được giữ nguyên."))return;
        var id=selected?.Id??0;var name=NameBox.Text;var active=ActiveBox.Checked;SetBusy(true);
        try
        {
            var rows=await workspace.SaveLookup(kind,id,new(name,active));Display(rows);
            var saved=rows.SingleOrDefault(x=>id>0?x.Id==id:string.Equals(x.Name,name.Trim(),StringComparison.OrdinalIgnoreCase));
            if(saved is not null)
            {
                var row=Grid.Rows.Cast<DataGridViewRow>().Single(r=>r.Tag is LookupRow value&&value.Id==saved.Id);
                Grid.CurrentCell=row.Cells[0];Grid.ClearSelection();row.Selected=true;selected=saved;
            }
            NameBox.Text=saved?.Name??name;ActiveBox.Checked=saved?.Active??active;MessageLabel.Text="Đã lưu và tải lại từ máy chủ.";
        }
        catch(Exception error){NameBox.Text=name;ActiveBox.Checked=active;MessageLabel.Text=AdminTheme.Failure(error);}finally{SetBusy(false);}
    }
    private void SetBusy(bool value){busy=value;SaveButton.Enabled=NewButton.Enabled=ReloadButton.Enabled=Grid.Enabled=NameBox.Enabled=ActiveBox.Enabled=!value;if(value)MessageLabel.Text="Đang xử lý…";}
}
