using PhoneStore.Admin.Client;

namespace PhoneStore.Admin;

public sealed class DashboardView:UserControl
{
    private readonly IAdminApi api;private readonly ReportsWorkspace workspace;private bool loading;
    public DateTimePicker FromBox { get; }=new(){Format=DateTimePickerFormat.Custom,CustomFormat="dd/MM/yyyy",Width=145,AccessibleName="Từ ngày tổng quan UTC+7"};
    public DateTimePicker ToBox { get; }=new(){Format=DateTimePickerFormat.Custom,CustomFormat="dd/MM/yyyy",Width=145,AccessibleName="Đến hết ngày tổng quan UTC+7"};
    public NumericUpDown ThresholdBox { get; }=new(){Minimum=0,Maximum=int.MaxValue,Width=80,AccessibleName="Ngưỡng sắp hết tổng quan"};
    public Button ReloadButton { get; }=AdminTheme.Button("&Tải tổng quan");public Label DateErrorLabel { get; }=AdminTheme.Label("");
    public IReadOnlyDictionary<string,Button> MetricButtons=>buttons;private readonly Dictionary<string,Button> buttons=[];
    public IReadOnlyDictionary<string,Label> MetricLabels=>labels;private readonly Dictionary<string,Label> labels=[];
    public ReportChart Chart { get; }=new(){MinimumSize=new(240,140)};public Label ChartTimeLabel { get; }=AdminTheme.Label("");
    public DashboardView(IAdminApi api)
    {
        this.api=api;workspace=new(api);Dock=DockStyle.Fill;var today=ReportPresentation.Today();FromBox.Value=today.AddDays(-29).ToDateTime(TimeOnly.MinValue);ToBox.Value=today.ToDateTime(TimeOnly.MinValue);ThresholdBox.Value=new StockPreferenceStore().Load();
        var root=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=5};root.RowStyles.Add(new RowStyle(SizeType.AutoSize));root.RowStyles.Add(new RowStyle(SizeType.AutoSize));root.RowStyles.Add(new RowStyle(SizeType.AutoSize));root.RowStyles.Add(new RowStyle(SizeType.Percent,100));root.RowStyles.Add(new RowStyle(SizeType.AutoSize));var filters=CatalogUi.Toolbar(AdminTheme.Label("Kỳ UTC+7:"),FromBox,ToBox,AdminTheme.Label("Sắp hết ≤"),ThresholdBox,ReloadButton);filters.Padding=new Padding(4);root.Controls.Add(filters,0,0);root.Controls.Add(DateErrorLabel,0,1);
        var metrics=new TableLayoutPanel{Dock=DockStyle.Fill,AutoSize=true,ColumnCount=2,RowCount=6};metrics.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,38));metrics.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,62));
        foreach(var(key,title)in new[]{("waiting","Đơn chờ xác nhận"),("shipping","Đơn đang giao"),("returns","Cần nhận lại"),("period","Doanh thu kỳ"),("today","Doanh thu hôm nay"),("stock","Sắp hết")})
        {
            var button=AdminTheme.Button(title,false);button.Dock=DockStyle.Fill;button.MinimumSize=new Size(150,30);button.Padding=new Padding(8,2,8,2);button.Margin=new Padding(0,2,8,2);var label=AdminTheme.Label("Chưa tải.");label.Margin=new Padding(0,6,0,6);label.MaximumSize=new Size(460,0);buttons.Add(key,button);labels.Add(key,label);var row=buttons.Count-1;metrics.RowStyles.Add(new RowStyle(SizeType.AutoSize));metrics.Controls.Add(button,0,row);metrics.Controls.Add(label,1,row);
            button.Click+=async(_,_)=>{if(key is "waiting" or "shipping" or "returns")await PageNavigation.OpenAsync(this,new OrderListForm(api,key=="waiting"?"CHO_XAC_NHAN":key=="shipping"?"DANG_GIAO":"GIAO_THAT_BAI"));else if(key=="stock")await PageNavigation.OpenAsync(this,new StockListForm(api,(int)ThresholdBox.Value,initialLow:true));else{var form=new ReportScreen(api);form.FromBox.Value=key=="today"?today.ToDateTime(TimeOnly.MinValue):FromBox.Value;form.ToBox.Value=key=="today"?today.ToDateTime(TimeOnly.MinValue):ToBox.Value;await PageNavigation.OpenAsync(this,form);}};
        }
        ChartTimeLabel.MaximumSize=new Size(900,0);root.Controls.Add(metrics,0,2);root.Controls.Add(Chart,0,3);root.Controls.Add(ChartTimeLabel,0,4);Controls.Add(root);ReloadButton.Click+=async(_,_)=>await LoadAsync();
        ThresholdBox.ValueChanged+=(_,_)=>InvalidateMetric("stock");FromBox.ValueChanged+=(_,_)=>InvalidateMetric("period");ToBox.ValueChanged+=(_,_)=>InvalidateMetric("period");
    }
    private void InvalidateMetric(string key){if(loading)return;buttons[key].Enabled=false;labels[key].Text="Điều kiện đã đổi. Chọn Tải tổng quan để cập nhật.";}
    public async Task LoadAsync()
    {
        if(loading)return;var from=DateOnly.FromDateTime(FromBox.Value);var to=DateOnly.FromDateTime(ToBox.Value);DateErrorLabel.Text=ReportPresentation.DateError("revenue",from,to)??"";DateErrorLabel.Visible=DateErrorLabel.Text.Length>0;if(DateErrorLabel.Text.Length>0)return;loading=true;ReloadButton.Enabled=FromBox.Enabled=ToBox.Enabled=ThresholdBox.Enabled=false;var threshold=(int)ThresholdBox.Value;var today=ReportPresentation.Today();
        async Task Count(string key,Func<Task<LoadedMetric<int>>> read)
        {labels[key].Text="Đang tải…";buttons[key].Enabled=false;try{var value=await read();labels[key].Text=$"{value.Value:N0} · tải lúc {AdminFormatting.Time(value.LoadedAt)}";buttons[key].Enabled=true;}catch(Exception error){labels[key].Text=AdminTheme.Failure(error);}}
        async Task Money(string key,DateOnly start,DateOnly end)
        {labels[key].Text="Đang tải…";buttons[key].Enabled=false;try{var value=await workspace.Revenue(start,end);labels[key].Text=$"{AdminFormatting.Money(value.Value.Revenue)} · tải lúc {AdminFormatting.Time(value.LoadedAt)}";buttons[key].Enabled=true;}catch(Exception error){labels[key].Text=AdminTheme.Failure(error);}}
        async Task SevenDays()
        {ChartTimeLabel.Text="Đang tải doanh thu 7 ngày…";try{var value=await workspace.Revenue(today.AddDays(-6),today);Chart.Bind(ReportPresentation.SevenDays(value.Value,today),"Doanh thu 7 ngày");ChartTimeLabel.Text=$"UTC+7 · tải lúc {AdminFormatting.Time(value.LoadedAt)}. Các chỉ số được tải riêng, không phải ảnh chụp nguyên tử.";}catch(Exception error){Chart.Bind([],"Chưa tải được doanh thu");ChartTimeLabel.Text=AdminTheme.Failure(error);}}
        try{await Task.WhenAll(Count("waiting",()=>workspace.Orders("CHO_XAC_NHAN")),Count("shipping",()=>workspace.Orders("DANG_GIAO")),Count("returns",()=>workspace.Unreceived()),Count("stock",()=>workspace.LowStock(threshold)),Money("period",from,to),Money("today",today,today),SevenDays());}
        finally{loading=false;ReloadButton.Enabled=FromBox.Enabled=ToBox.Enabled=ThresholdBox.Enabled=true;}
    }
}
