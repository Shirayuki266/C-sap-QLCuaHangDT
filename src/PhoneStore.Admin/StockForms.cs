using System.Text.Json;
using PhoneStore.Admin.Client;

namespace PhoneStore.Admin;

public sealed class StockPreferenceStore(string? directory=null)
{
    private readonly string directory=directory??Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"PhoneStore.Admin");
    public int Load(){try{var file=Path.Combine(directory,"stock-settings.json");if(!File.Exists(file))return 5;var value=JsonSerializer.Deserialize<Threshold>(File.ReadAllText(file));return value is{Below:>=0}?value.Below:5;}catch(Exception e)when(e is IOException or UnauthorizedAccessException or JsonException){return 5;}}
    public void Save(int value){if(value<0)throw new ArgumentOutOfRangeException(nameof(value));Directory.CreateDirectory(directory);var path=Path.Combine(directory,"stock-settings.json");File.WriteAllText(path+".tmp",JsonSerializer.Serialize(new Threshold(value)));File.Move(path+".tmp",path,true);}
    private sealed record Threshold(int Below);
}

public sealed class StockListForm:Form
{
    private readonly IAdminApi api;private readonly SupplyWorkspace workspace;private readonly Action<int>? saveThreshold;private int page=1,total;private bool busy;
    public TextBox QueryBox { get; }=AdminTheme.Input("Tìm SKU, tên hoặc màu");public ComboBox BrandBox { get; }=new(){DropDownStyle=ComboBoxStyle.DropDownList,Width=190,AccessibleName="Hãng"};
    public CheckBox BelowBox { get; }=new(){Text="Chỉ sắp hết, khả dụng ≤",AutoSize=true};public NumericUpDown ThresholdBox { get; }=new(){Minimum=0,Maximum=int.MaxValue,Width=90,AccessibleName="Ngưỡng sắp hết tường minh"};
    public DataGridView Grid { get; }=CatalogUi.Grid();public Button SearchButton { get; }=AdminTheme.Button("Tìm kiếm");public Button CountButton { get; }=AdminTheme.Button("Kiểm kê",false);public Button JournalButton { get; }=AdminTheme.Button("Nhật ký kho",false);
    public Button PreviousButton { get; }=AdminTheme.Button("Trang trước",false);public Button NextButton { get; }=AdminTheme.Button("Trang sau",false);public Label MessageLabel { get; }=AdminTheme.Label("");
    public StockListForm(IAdminApi api,int threshold=5,Action<int>? saveThreshold=null)
    {
        this.api=api;workspace=new(api);this.saveThreshold=saveThreshold;AdminTheme.Frame(this,"Tồn kho");QueryBox.Dock=DockStyle.None;QueryBox.Width=240;QueryBox.MaxLength=100;ThresholdBox.Value=Math.Max(0,threshold);Grid.ReadOnly=true;
        foreach(var(p,t)in new[]{("Sku","SKU"),("Name","Điện thoại"),("Variant","Phiên bản"),("OnHand","Tồn thực"),("Reserved","Đang giữ"),("Available","Có thể bán")})CatalogUi.Column(Grid,p,t,true,p=="Name"?260:150);
        CatalogUi.Layout(this,"Tồn kho",CatalogUi.Toolbar(QueryBox,BrandBox,BelowBox,ThresholdBox,SearchButton,CountButton,JournalButton),Grid,CatalogUi.Toolbar(PreviousButton,NextButton,MessageLabel));
        SearchButton.Click+=async(_,_)=>{page=1;await LoadAsync();};PreviousButton.Click+=async(_,_)=>{if(!busy&&page>1){page--;await LoadAsync();}};NextButton.Click+=async(_,_)=>{if(!busy&&page*20<total){page++;await LoadAsync();}};
        CountButton.Click+=async(_,_)=>{if(Grid.CurrentRow?.Tag is StockRecord row){using var form=new StockCountDialog(api,row);form.ShowDialog(this);await LoadAsync();}};JournalButton.Click+=(_,_)=>{if(Grid.CurrentRow?.Tag is StockRecord row){using var form=new StockJournalForm(api,row);form.ShowDialog(this);}};
        Grid.SelectionChanged+=(_,_)=>Actions();ThresholdBox.ValueChanged+=(_,_)=>{try{this.saveThreshold?.Invoke((int)ThresholdBox.Value);}catch(Exception){MessageLabel.Text="Chưa lưu được ngưỡng trong cấu hình người dùng; vẫn có thể tìm theo giá trị đang nhập.";}};Shown+=async(_,_)=>await LoadAsync();
    }
    private void Actions(){SearchButton.Enabled=QueryBox.Enabled=BrandBox.Enabled=BelowBox.Enabled=ThresholdBox.Enabled=Grid.Enabled=!busy;CountButton.Enabled=JournalButton.Enabled=!busy&&Grid.CurrentRow?.Tag is StockRecord;PreviousButton.Enabled=!busy&&page>1;NextButton.Enabled=!busy&&page*20<total;}
    public async Task LoadAsync()
    {
        if(busy)return;busy=true;Actions();MessageLabel.Text="Đang tải tồn kho…";
        try
        {
            var selected=(BrandBox.SelectedItem as LookupRow)?.Id??0;var brands=await new CatalogWorkspace(api).Lookups("brands");BrandBox.DataSource=new[]{new LookupRow(0,"Tất cả hãng",true)}.Concat(brands).ToList();BrandBox.SelectedItem=((List<LookupRow>)BrandBox.DataSource).SingleOrDefault(b=>b.Id==selected);
            var batch=await workspace.Stock(QueryBox.Text,selected>0?selected:null,BelowBox.Checked?(int)ThresholdBox.Value:null,page);total=batch.Total;Grid.Rows.Clear();foreach(var row in batch.Items){var index=Grid.Rows.Add(row.Sku,row.Name,$"{row.Color} · {row.StorageGb} GB",row.OnHand,row.Reserved,row.Available);Grid.Rows[index].Tag=row;}MessageLabel.Text=total==0?"Không có biến thể phù hợp. Thử đổi bộ lọc.":$"Trang {page} · {total} biến thể · Tồn và khả dụng từ máy chủ";
        }
        catch(Exception error){Grid.Rows.Clear();total=0;MessageLabel.Text=SupplyPresentation.Failure(error);}finally{busy=false;Actions();}
    }
}

public sealed class StockCountDialog:Form
{
    private readonly SupplyWorkspace workspace;private readonly IAdminApi api;private readonly Func<string,string,bool> confirm;private StockRecord stock;private CountOperation? operation;private bool busy;
    public NumericUpDown CountedBox { get; }=new(){Minimum=0,Maximum=int.MaxValue,Dock=DockStyle.Fill,AccessibleName="Số thực tế tuyệt đối"};
    public TextBox ReasonBox { get; }=AdminTheme.Input("Lý do kiểm kê");public Label SummaryLabel { get; }=AdminTheme.Label("");public Label MessageLabel { get; }=AdminTheme.Label("");
    public Button SubmitButton { get; }=AdminTheme.Button("Ghi nhận kiểm kê");public Button ReloadButton { get; }=AdminTheme.Button("Tải lại / đối soát",false);
    public Guid? OperationKey=>operation?.Intent.Key;
    public StockCountDialog(IAdminApi api,StockRecord stock,Func<string,string,bool>? confirmation=null)
    {
        this.api=api;this.stock=stock;workspace=new(api);confirm=confirmation??((t,m)=>CatalogUi.Confirm(this,t,m));AdminTheme.Frame(this,"Kiểm kê tồn kho");ReasonBox.MaxLength=1000;ReasonBox.Multiline=true;ReasonBox.Height=110;CountedBox.Value=stock.OnHand;
        var body=AdminTheme.Stack(28);body.Dock=DockStyle.Top;AdminTheme.Add(body,AdminTheme.Label("Kiểm kê tồn kho",22,true));AdminTheme.Add(body,SummaryLabel);AdminTheme.Add(body,AdminTheme.Label("Số thực tế tuyệt đối *"));AdminTheme.Add(body,CountedBox);AdminTheme.Field(body,"Lý do *",ReasonBox);AdminTheme.Add(body,MessageLabel);AdminTheme.Add(body,CatalogUi.Toolbar(SubmitButton,ReloadButton));Controls.Add(body);
        CountedBox.ValueChanged+=(_,_)=>Summary();SubmitButton.Click+=async(_,_)=>await SubmitAsync();ReloadButton.Click+=async(_,_)=>await ResolveAsync();Summary();Actions();
    }
    private void Summary()=>SummaryLabel.Text=SupplyPresentation.CountSummary(stock,(int)CountedBox.Value);
    private void Actions(){var pending=operation?.NeedsReadback==true||api.NeedsReconciliation;SubmitButton.Enabled=!busy&&!pending&&operation?.Finished!=true;CountedBox.Enabled=ReasonBox.Enabled=!busy&&!pending&&operation?.Finished!=true;ReloadButton.Enabled=!busy;}
    public async Task SubmitAsync()
    {
        if(busy||!SubmitButton.Enabled)return;var counted=(int)CountedBox.Value;var invalid=SupplyPresentation.CountError(stock,counted,ReasonBox.Text);if(invalid is not null){MessageLabel.Text=invalid;return;}
        if(!confirm("Xác nhận kiểm kê",SupplyPresentation.CountSummary(stock,counted)+"\nLý do: "+ReasonBox.Text.Trim()+"\nMáy chủ kiểm tra phiên bản và số đang giữ trước khi lưu."))return;
        if(operation is null||operation.Counted!=counted||operation.Reason!=ReasonBox.Text.Trim())operation=new(stock,counted,ReasonBox.Text);
        busy=true;Actions();MessageLabel.Text="Đang ghi nhận kiểm kê…";
        try{stock=await workspace.Adjust(operation);Summary();MessageLabel.Text="Đã ghi nhận. Tồn hiện tại đã tải từ máy chủ.";}
        catch(Exception error){MessageLabel.Text=SupplyPresentation.Failure(error)+(operation.NeedsReadback?" Chọn đối soát để xác định kết quả mới.":"");}finally{busy=false;Actions();}
    }
    public async Task ResolveAsync()
    {
        if(busy)return;busy=true;Actions();MessageLabel.Text="Đang đối soát nhật ký và tồn kho…";
        try
        {
            if(operation is not null){var result=await workspace.Resolve(operation);stock=result.Stock;if(!result.Applied&&!result.RetryAllowed)operation=null;MessageLabel.Text=result.Applied?"Nhật ký đã ghi nhận thao tác. Không gửi lại kiểm kê.":result.RetryAllowed?"Chưa có nhật ký, phiên bản không đổi. Có thể thử lại cùng thao tác.":"Tồn đã thay đổi. Kiểm tra số mới và xác nhận lại nếu còn cần điều chỉnh.";}
            else{stock=await workspace.StockRow(stock.VariantId,stock.Sku);MessageLabel.Text="Đã tải lại tồn kho.";}Summary();
        }
        catch(Exception error){MessageLabel.Text=SupplyPresentation.Failure(error);}finally{busy=false;Actions();}
    }
}

public sealed class StockJournalForm:Form
{
    private readonly SupplyWorkspace workspace;private readonly StockRecord stock;private int page=1,total;private bool busy;
    public DataGridView Grid { get; }=CatalogUi.Grid();public Label MessageLabel { get; }=AdminTheme.Label("");public Button PreviousButton { get; }=AdminTheme.Button("Trang trước",false);public Button NextButton { get; }=AdminTheme.Button("Trang sau",false);
    public StockJournalForm(IAdminApi api,StockRecord stock)
    {
        this.stock=stock;workspace=new(api);AdminTheme.Frame(this,"Nhật ký kho");Grid.ReadOnly=true;foreach(var(p,t)in new[]{("At","Thời gian UTC+7"),("Kind","Loại"),("DeltaH","Chênh tồn"),("DeltaR","Chênh giữ"),("Document","Chứng từ"),("Actor","Người thực hiện"),("Reason","Lý do")})CatalogUi.Column(Grid,p,t,true,p=="At"?200:150);
        CatalogUi.Layout(this,"Nhật ký · "+stock.Sku,CatalogUi.Toolbar(AdminTheme.Label(stock.Name)),Grid,CatalogUi.Toolbar(PreviousButton,NextButton,MessageLabel));Shown+=async(_,_)=>await LoadAsync();PreviousButton.Click+=async(_,_)=>{if(!busy&&page>1){page--;await LoadAsync();}};NextButton.Click+=async(_,_)=>{if(!busy&&page*20<total){page++;await LoadAsync();}};
    }
    public async Task LoadAsync()
    {
        if(busy)return;busy=true;PreviousButton.Enabled=NextButton.Enabled=false;MessageLabel.Text="Đang tải nhật ký…";
        try{var batch=await workspace.Movements(stock.VariantId,page:page);total=batch.Total;Grid.Rows.Clear();foreach(var row in batch.Items)Grid.Rows.Add(AdminFormatting.Time(row.At),SupplyPresentation.Movement(row.Kind),row.DeltaOnHand,row.DeltaReserved,row.PurchaseId.HasValue?$"Phiếu nhập #{row.PurchaseId}":row.OrderId.HasValue?$"Đơn #{row.OrderId}":"Kiểm kê",row.Actor,row.Reason??"");MessageLabel.Text=total==0?"Biến thể chưa có nhật ký kho.":$"Trang {page} · {total} dòng nhật ký";}
        catch(Exception error){Grid.Rows.Clear();MessageLabel.Text=SupplyPresentation.Failure(error);}finally{busy=false;PreviousButton.Enabled=page>1;NextButton.Enabled=page*20<total;}
    }
}
