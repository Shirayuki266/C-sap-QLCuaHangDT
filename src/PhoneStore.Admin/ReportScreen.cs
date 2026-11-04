using PhoneStore.Admin.Client;

namespace PhoneStore.Admin;

public sealed class ReportChart:UserControl
{
    public Control Canvas { get; }
    public Label SelectionLabel { get; }=AdminTheme.Label("Chọn cột để xem nhãn và giá trị.");
    public IReadOnlyList<ReportPoint> Points { get; private set; }=[];
    public ReportChart()
    {
        Canvas=new ChartSurface(this){Dock=DockStyle.Fill,BackColor=Color.White,AccessibleName="Biểu đồ từ kết quả báo cáo",TabStop=true};Dock=DockStyle.Fill;MinimumSize=new(240,160);var layout=new TableLayoutPanel{Dock=DockStyle.Fill,RowCount=2,ColumnCount=1};layout.RowStyles.Add(new RowStyle(SizeType.Percent,100));layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));layout.Controls.Add(Canvas,0,0);layout.Controls.Add(SelectionLabel,0,1);Controls.Add(layout);
        Canvas.MouseDown+=(_,e)=>{if(e.Button!=MouseButtons.Left)return;foreach(var(index,rect)in bars)if(rect.Contains(e.Location)){SelectPoint(index);break;}};
        Canvas.KeyDown+=(_,e)=>{if(e.KeyCode is Keys.Left or Keys.Right&&Points.Count>0){SelectPoint(Math.Clamp(selected+(e.KeyCode==Keys.Right?1:-1),0,Points.Count-1));e.Handled=true;}};
    }
    public void Bind(IReadOnlyList<ReportPoint> points,string title)
    {
        Points=points;this.title=title;selected=-1;bars.Clear();
        SelectionLabel.Text=points.Count==0?"Không có dữ liệu biểu đồ.":"Chọn cột hoặc dòng trong bảng để xem nhãn và giá trị.";Canvas.Refresh();
    }
    private readonly Dictionary<int,RectangleF> bars=[];private string title="";private int selected=-1;
    public IReadOnlyDictionary<int,RectangleF> BarBounds=>bars;
    public void SelectPoint(int index){if(index>=0&&index<Points.Count){selected=index;SelectionLabel.Text=Points[index].Label+" · "+Points[index].Display;Canvas.Invalidate();}}
    private sealed class ChartSurface(ReportChart owner):Control
    {
        protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);owner.Draw(e.Graphics,ClientRectangle);}
        protected override void OnResize(EventArgs e){base.OnResize(e);Invalidate();}
    }
    private void Draw(Graphics graphics,Rectangle bounds)
    {
        bars.Clear();if(bounds.Width<160||bounds.Height<70)return;graphics.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias;using var ink=new SolidBrush(AdminTheme.Ink);using var blue=new SolidBrush(AdminTheme.Blue);using var chosen=new SolidBrush(AdminTheme.Navy);using var grid=new Pen(Color.FromArgb(210,218,232));using var font=new Font("Segoe UI",9);using var heading=new Font("Segoe UI",10,FontStyle.Bold);
        graphics.DrawString(title,heading,ink,8,4);var plot=new RectangleF(80,31,bounds.Width-96,Math.Max(10,bounds.Height-73));graphics.DrawLine(grid,plot.Left,plot.Bottom,plot.Right,plot.Bottom);
        if(Points.Count==0){graphics.DrawString("Không có dữ liệu biểu đồ.",font,ink,plot.Left,plot.Top);return;}
        var max=Math.Max(1,Points.Max(p=>p.Value));var currency=title.Contains("Doanh thu",StringComparison.OrdinalIgnoreCase);var unit=currency?1000000m:1m;graphics.DrawString((max/unit).ToString("N1",System.Globalization.CultureInfo.GetCultureInfo("vi-VN"))+(currency?" tr":""),font,ink,2,plot.Top);graphics.DrawString("0",font,ink,50,plot.Bottom-12);
        var slot=plot.Width/Points.Count;var stride=Math.Max(1,(int)Math.Ceiling(Points.Count*90.0/plot.Width));
        for(var i=0;i<Points.Count;i++){var height=(float)(Points[i].Value/max)*plot.Height;var rect=new RectangleF(plot.Left+i*slot+slot*.12f,plot.Bottom-height,Math.Max(1,slot*.76f),Math.Max(1,height));bars[i]=rect;graphics.FillRectangle(i==selected?chosen:blue,rect);if(i%stride==0){using var format=new StringFormat{Trimming=StringTrimming.EllipsisCharacter,FormatFlags=StringFormatFlags.NoWrap};graphics.DrawString(Points[i].Label,font,ink,new RectangleF(plot.Left+i*slot,plot.Bottom+4,Math.Max(1,slot*stride-3),30),format);}}
    }
}

public sealed class ReportScreen:NavigationPageForm
{
    private readonly ReportsWorkspace workspace;private bool busy;private int page=1;
    public AdminReport? Current { get; private set; }public ReportProjection? Projection { get; private set; }
    public ComboBox KindBox { get; }=new(){DropDownStyle=ComboBoxStyle.DropDownList,Width=210,AccessibleName="Loại báo cáo"};
    public DateTimePicker FromBox { get; }=DateInput("Từ ngày UTC+7");public DateTimePicker ToBox { get; }=DateInput("Đến hết ngày UTC+7");
    public CheckBox MonthlyBox { get; }=new(){Text="Nhóm theo tháng",AutoSize=true,AccessibleName="Nhóm doanh thu theo tháng"};
    public Button LoadButton { get; }=AdminTheme.Button("&Tải báo cáo");public Button PreviousButton { get; }=AdminTheme.Button("Trang trước",false);public Button NextButton { get; }=AdminTheme.Button("Trang sau",false);
    public Label DateErrorLabel { get; }=AdminTheme.Label("");public Label SummaryLabel { get; }=AdminTheme.Label("");public Label MessageLabel { get; }=AdminTheme.Label("");public DataGridView Grid { get; }=CatalogUi.Grid();public ReportChart Chart { get; }=new();
    private static DateTimePicker DateInput(string name)=>new(){Format=DateTimePickerFormat.Custom,CustomFormat="dd/MM/yyyy",ShowCheckBox=true,Checked=true,Width=150,AccessibleName=name};
    private string Kind=>KindBox.SelectedIndex switch{1=>"bestsellers",2=>"stock",_=>"revenue"};
    public ReportScreen(IAdminApi api)
    {
        workspace=new(api);AdminTheme.Frame(this,"Báo cáo");KindBox.Items.AddRange(["Doanh thu","Bán chạy theo biến thể","Tồn hiện tại"]);KindBox.SelectedIndex=0;FromBox.Value=ReportPresentation.Today().AddDays(-6).ToDateTime(TimeOnly.MinValue);ToBox.Value=ReportPresentation.Today().ToDateTime(TimeOnly.MinValue);Grid.ReadOnly=true;
        var root=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=8,Padding=new Padding(16)};root.RowStyles.Add(new RowStyle(SizeType.AutoSize));root.RowStyles.Add(new RowStyle(SizeType.AutoSize));root.RowStyles.Add(new RowStyle(SizeType.AutoSize));root.RowStyles.Add(new RowStyle(SizeType.AutoSize));root.RowStyles.Add(new RowStyle(SizeType.Percent,48));root.RowStyles.Add(new RowStyle(SizeType.Percent,52));root.RowStyles.Add(new RowStyle(SizeType.AutoSize));root.RowStyles.Add(new RowStyle(SizeType.AutoSize));root.Controls.Add(AdminTheme.Label("Báo cáo",22,true),0,0);root.Controls.Add(CatalogUi.Toolbar(KindBox,AdminTheme.Label("Từ ngày"),FromBox,AdminTheme.Label("Đến hết ngày"),ToBox,MonthlyBox,LoadButton),0,1);root.Controls.Add(DateErrorLabel,0,2);root.Controls.Add(SummaryLabel,0,3);root.Controls.Add(Chart,0,4);root.Controls.Add(Grid,0,5);root.Controls.Add(MessageLabel,0,6);root.Controls.Add(CatalogUi.Toolbar(PreviousButton,NextButton),0,7);Controls.Add(root);
        LoadButton.Click+=async(_,_)=>await LoadAsync();KindBox.SelectedIndexChanged+=(_,_)=>{Current=null;Projection=null;Grid.Rows.Clear();Chart.Bind([],"Chọn Tải báo cáo");SummaryLabel.Text="";Actions();};MonthlyBox.CheckedChanged+=(_,_)=>{if(Current is null)return;page=1;Projection=ReportPresentation.Project(Current,MonthlyBox.Checked);RenderPage();};PreviousButton.Click+=(_,_)=>{if(!busy&&page>1){page--;RenderPage();}};NextButton.Click+=(_,_)=>{if(!busy&&page*20<(Projection?.Rows.Count??0)){page++;RenderPage();}};Grid.SelectionChanged+=(_,_)=>{if(Grid.CurrentRow is{} row)Chart.SelectPoint(row.Index);};ConfigurePageLoad(LoadAsync,()=>busy);AcceptButton=LoadButton;Actions();
    }
    private void Actions(){PageNavigation.StateChanged(this);LoadButton.Enabled=KindBox.Enabled=!busy;FromBox.Enabled=ToBox.Enabled=!busy&&Kind!="stock";MonthlyBox.Enabled=!busy&&Kind=="revenue";PreviousButton.Enabled=!busy&&page>1;NextButton.Enabled=!busy&&page*20<(Projection?.Rows.Count??0);}
    private void RenderPage()
    {
        if(Projection is null)return;Grid.Rows.Clear();Grid.Columns.Clear();var kind=Projection.Source.Kind;
        foreach(var(p,t)in(kind=="revenue"?new[]{("Key","Ngày / tháng UTC+7"),("Orders","Đơn hoàn tất"),("Revenue","Doanh thu (đ)")}:kind=="bestsellers"?new[]{("Key","SKU biến thể"),("Name","Điện thoại"),("Quantity","Số lượng"),("Revenue","Doanh thu (đ)")}:new[]{("Key","SKU biến thể"),("Name","Điện thoại"),("OnHand","Tồn thực"),("Reserved","Đang giữ"),("Available","Có thể bán")}))CatalogUi.Column(Grid,p,t,true,p=="Name"?260:190);
        var rows=Projection.Rows.Skip((page-1)*20).Take(20).ToArray();foreach(var row in rows){if(kind=="revenue")Grid.Rows.Add(row.Key,row.Orders,AdminFormatting.Money(row.Revenue));else if(kind=="bestsellers")Grid.Rows.Add(row.Key,row.Name,row.Quantity,AdminFormatting.Money(row.Revenue));else Grid.Rows.Add(row.Key,row.Name,row.OnHand,row.Reserved,row.Available);}
        Chart.Bind(Projection.Points.Skip((page-1)*20).Take(20).ToArray(),kind=="revenue"?"Doanh thu":kind=="bestsellers"?"Số lượng bán theo biến thể":"Khả dụng hiện tại");SummaryLabel.Text=ReportPresentation.Summary(Projection.Source);MessageLabel.Text=rows.Length==0?"Không có dữ liệu. Các tổng từ máy chủ bằng 0.":$"Trang {page} · {Projection.Rows.Count} dòng · Bảng và biểu đồ từ cùng một lần tải.";Actions();
    }
    public async Task LoadAsync()
    {
        if(busy)return;DateOnly? from=FromBox.Checked?DateOnly.FromDateTime(FromBox.Value):null,to=ToBox.Checked?DateOnly.FromDateTime(ToBox.Value):null;DateErrorLabel.Text=ReportPresentation.DateError(Kind,from,to)??"";DateErrorLabel.Visible=DateErrorLabel.Text.Length>0;if(DateErrorLabel.Text.Length>0)return;
        busy=true;Actions();MessageLabel.Text="Đang tải báo cáo…";
        try{Current=await workspace.Read(Kind,from,to);Projection=ReportPresentation.Project(Current,MonthlyBox.Checked&&Kind=="revenue");page=Math.Clamp(page,1,Math.Max(1,(Projection.Rows.Count+19)/20));RenderPage();}
        catch(Exception error){Current=null;Projection=null;Grid.Rows.Clear();Chart.Bind([],"Chưa tải được dữ liệu");SummaryLabel.Text="Chưa có số liệu xác nhận.";MessageLabel.Text=AdminTheme.Failure(error);}finally{busy=false;Actions();}
    }
}
