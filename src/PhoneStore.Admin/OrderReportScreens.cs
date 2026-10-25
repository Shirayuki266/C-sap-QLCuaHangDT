using System.Drawing.Printing;
using PhoneStore.Core.Contracts;
namespace PhoneStore.Admin;
public sealed class OrderDetailForm:Form
{
    private readonly ApiClient api;private readonly long id;private OrderDto? order;
    private readonly Label info=new(){Dock=DockStyle.Top,Height=150,Padding=new(14)};
    private readonly FlowLayoutPanel actions=new(){Dock=DockStyle.Bottom,Height=70,Padding=new(12)};
    private readonly DataGridView lines=Ui.Grid();private readonly DataGridView history=Ui.Grid();
    public OrderDetailForm(ApiClient client,long orderId) {
        api=client;id=orderId;Text="Chi tiết và xử lý đơn hàng";Size=new(1200,800);MinimumSize=new(1000,650);StartPosition=FormStartPosition.CenterParent;Font=new("Segoe UI",10);
        var split=new SplitContainer{Dock=DockStyle.Fill,Orientation=Orientation.Horizontal,SplitterDistance=270};split.Panel1.Controls.Add(lines);split.Panel2.Controls.Add(history);Controls.Add(split);Controls.Add(actions);Controls.Add(info);
        Shown+=async(_,_)=>await LoadOrder();
    }
    private async Task LoadOrder() {try{order=await api.Get<OrderDto>($"/api/admin/orders/{id}");info.Text=$"{order.Code} · {Labels.Status(order.Status)} · {Labels.Status(order.Payment)}\nKhách hàng: {order.Customer}\nNgười nhận: {order.Recipient} · {order.Phone}\nĐịa chỉ: {order.Address}\nTổng tiền COD: {order.Total.FormatMoney()} ₫ · Đã nhận lại: {(order.ReturnReceived?"Có":"Chưa")}";lines.DataSource=order.Lines;history.DataSource=order.History;Ui.Format(lines);Ui.Format(history);BuildActions();}catch(Exception e){MessageBox.Show(e.Message,"Lỗi tải đơn");}}
    private void BuildActions() {
        actions.Controls.Clear();if(order==null)return;
        void Add(string label,string action){var button=new Button{Text=label,AutoSize=true,Padding=new(8,5,8,5)};actions.Controls.Add(button);button.Click+=async(_,_)=>await Execute(action,label);}
        switch(order.Status){case "CHO_XAC_NHAN":Add("Xác nhận đơn","confirm");Add("Hủy đơn","cancel");break;case "DA_XAC_NHAN":Add("Xuất giao","dispatch");Add("Hủy đơn","cancel");break;case "DANG_GIAO":Add("Hoàn tất và thu COD","complete");Add("Giao thất bại","fail");break;case "GIAO_THAT_BAI" when !order.ReturnReceived:Add("Nhận lại toàn bộ hàng thực tế","return");break;}
        var print=new Button{Text="In hóa đơn bán hàng",AutoSize=true,Enabled=order.Status is "DA_XAC_NHAN" or "DANG_GIAO" or "HOAN_TAT"};actions.Controls.Add(print);print.Click+=(_,_)=>PrintInvoice();
        var refresh=new Button{Text="Tải lại",AutoSize=true};actions.Controls.Add(refresh);refresh.Click+=async(_,_)=>await LoadOrder();
    }
    private async Task Execute(string action,string title) {
        if(order==null)return;var key=Guid.NewGuid();var version=order.Version;
        if(action is "cancel" or "fail") {
            using var dialog=new EditDialog(title,[new("reason","Lý do bắt buộc","",Multiline:true)],async d=>{
                if(!Ui.Confirm(title+" cho đơn "+order.Code+"?"))throw new InvalidOperationException("Chưa xác nhận thao tác.");
                await api.Send<bool>($"/api/admin/orders/{id}/{action}",new TransitionRequest(version,key,d.Value("reason")));
            });dialog.ShowDialog(this);await LoadOrder();return;
        }
        if(!Ui.Confirm(title+" cho đơn "+order.Code+ (action=="complete"?$" và ghi nhận đã thu đủ {order.Total.FormatMoney()} ₫?":action=="return"?"? Chỉ xác nhận khi thực tế đã nhận đầy đủ hàng.":"?")))return;
        actions.Enabled=false;UseWaitCursor=true;
        try{await api.Send<bool>($"/api/admin/orders/{id}/{action}",new TransitionRequest(version,key));await LoadOrder();}catch(Exception e){MessageBox.Show(e.Message,"Không thể xử lý");await LoadOrder();}finally{actions.Enabled=true;UseWaitCursor=false;}
    }
    private void PrintInvoice() {
        if(order==null)return;
        var text=new List<string>{"PHONE STORE — HÓA ĐƠN BÁN HÀNG", "Chứng từ bán hàng, không phải hóa đơn điện tử thuế",order.Code,"Ngày đặt: "+Labels.Local(order.CreatedAt).ToString("dd/MM/yyyy HH:mm"),"Người nhận: "+order.Recipient+" · "+order.Phone,"Địa chỉ: "+order.Address,"Trạng thái: "+Labels.Status(order.Status)+" · "+Labels.Status(order.Payment),""};
        foreach(var line in order.Lines){text.Add(line.Name+" · "+line.Sku);text.Add(line.Variant+$" · {line.Quantity} × {line.Price.FormatMoney()} = {line.Total.FormatMoney()} ₫");}text.Add("");text.Add($"TỔNG COD: {order.Total.FormatMoney()} ₫");
        using var doc=new PrintDocument();using var font=new Font("Segoe UI",11);var position=0;
        doc.BeginPrint+=(_,_)=>position=0;doc.PrintPage+=(_,e)=>{float y=e.MarginBounds.Top;while(position<text.Count){var height=e.Graphics!.MeasureString(text[position],font,e.MarginBounds.Width).Height+10;if(y+height>e.MarginBounds.Bottom)break;e.Graphics.DrawString(text[position],font,Brushes.Black,new RectangleF(e.MarginBounds.Left,y,e.MarginBounds.Width,height));y+=height;position++;}e.HasMorePages=position<text.Count;};
        using var preview=new PrintPreviewDialog{Document=doc,Width=1100,Height=800};preview.ShowDialog(this);
    }
}
public sealed class ReportScreen:UserControl
{
    private readonly ApiClient api;private readonly bool dashboard;private readonly ComboBox kind=new(){DropDownStyle=ComboBoxStyle.DropDownList,Width=180};
    private readonly DateTimePicker from=new(){Format=DateTimePickerFormat.Short,Width=130};private readonly DateTimePicker to=new(){Format=DateTimePickerFormat.Short,Width=130};
    private readonly Label summary=new(){Dock=DockStyle.Top,Height=115,Padding=new(14)};private readonly DataGridView grid=Ui.Grid();private readonly Panel chartPanel=new(){Dock=DockStyle.Fill,AutoScroll=true,BackColor=Color.White};
    private readonly Button load=new(){Text="Xem báo cáo",AutoSize=true};[System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)] public string? LastError {get;private set;} private bool busy;
    public ReportScreen(ApiClient client,bool isDashboard=false,Action? pending=null,Action? lowStock=null) {
        api=client;dashboard=isDashboard;Dock=DockStyle.Fill;from.Value=DateTime.UtcNow.AddHours(7).Date.AddDays(-6);to.Value=DateTime.UtcNow.AddHours(7).Date;
        kind.DataSource=new[]{new{Id="revenue",Name="Doanh thu"},new{Id="bestsellers",Name="Sản phẩm bán chạy"},new{Id="stock",Name="Tồn khả dụng hiện tại"}};kind.DisplayMember="Name";kind.ValueMember="Id";
        var filters=new FlowLayoutPanel{Dock=DockStyle.Top,AutoSize=true,Padding=new(14)};filters.Controls.AddRange([kind,new Label{Text="Từ ngày",AutoSize=true},from,new Label{Text="Đến ngày",AutoSize=true},to,load]);
        if(isDashboard){kind.Enabled=false;var wait=new Button{Text="Đơn chờ xác nhận",AutoSize=true};wait.Click+=(_,_)=>pending?.Invoke();var low=new Button{Text="Hàng sắp hết",AutoSize=true};low.Click+=(_,_)=>lowStock?.Invoke();filters.Controls.AddRange([wait,low]);}
        var split=new SplitContainer{Dock=DockStyle.Fill,Orientation=Orientation.Horizontal,SplitterDistance=330};split.Panel1.Controls.Add(chartPanel);split.Panel2.Controls.Add(grid);Controls.Add(split);Controls.Add(summary);Controls.Add(filters);
        load.Click+=async(_,_)=>await RefreshData();kind.SelectedValueChanged+=(_,_)=>{var stock=(string?)kind.SelectedValue=="stock";from.Enabled=to.Enabled=!stock;};
    }
    public async Task RefreshData() {
        if(busy)return;busy=true;LastError=null;load.Enabled=false;summary.Text="Đang tổng hợp…";try{
            var type=(string?)kind.SelectedValue??"revenue";var report=await api.Get<ReportResult>($"/api/admin/reports?kind={type}&from={from.Value:yyyy-MM-dd}&to={to.Value:yyyy-MM-dd}");
            summary.Text=$"Tổng: {report.Total.FormatMoney()} {(type=="revenue"?"₫":"sản phẩm")} · {(report.Rows.Count==0?"Chưa có dữ liệu trong kỳ.":$"{report.Rows.Count} dòng dữ liệu")}\nBảng và biểu đồ dùng cùng kết quả. {(type=="stock"?"Cột Số lượng là đang giữ; Giá trị là khả dụng.":type=="revenue"?"Chỉ tính đơn hoàn tất và đã thu COD.":"Chỉ tính sản phẩm trong đơn hoàn tất và đã thu COD.")}";
            if(dashboard){var today=DateTime.UtcNow.AddHours(7).ToString("yyyy-MM-dd");var todayReport=await api.Get<ReportResult>($"/api/admin/reports?kind=revenue&from={today}&to={today}");var waiting=await api.Get<Page<OrderDto>>("/api/admin/orders?status=CHO_XAC_NHAN");var low=await api.Get<List<StockDto>>("/api/admin/stock?low=true");summary.Text+=$"\nDoanh thu hôm nay: {todayReport.Total.FormatMoney()} ₫ · Đơn chờ: {waiting.Total} · Biến thể sắp hết: {low.Count}";}
            grid.DataSource=report.Rows;Ui.Format(grid);chartPanel.Controls.Clear();chartPanel.Controls.Add(new BarChart(report.Rows){Width=Math.Max(chartPanel.ClientSize.Width-25,report.Rows.Count*140+80),Height=290});
        }catch(Exception e){LastError=e.Message;summary.Text="Lỗi: "+e.Message;}finally{busy=false;load.Enabled=true;}
    }
}
public sealed class BarChart:Control
{
    private readonly List<ReportRow> rows;private readonly ToolTip tips=new();
    public BarChart(List<ReportRow> values){rows=values;DoubleBuffered=true;BackColor=Color.White;MouseMove+=(_,e)=>{var index=(e.X-55)/140;if(index>=0&&index<rows.Count)tips.SetToolTip(this,$"{rows[index].Label}: {rows[index].Value.FormatMoney()}");};}
    protected override void Dispose(bool disposing){if(disposing)tips.Dispose();base.Dispose(disposing);}
    protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);if(rows.Count==0){e.Graphics.DrawString("Chưa có dữ liệu",Font,Brushes.Gray,30,30);return;}var max=Math.Max(1m,rows.Max(x=>x.Value));e.Graphics.DrawLine(Pens.Gray,50,230,Width-10,230);for(int i=0;i<rows.Count;i++){var r=rows[i];var height=(float)(r.Value/max*180);var x=55+i*140;e.Graphics.FillRectangle(Brushes.SteelBlue,x,230-height,80,height);e.Graphics.DrawString(r.Value.FormatMoney(),Font,Brushes.Black,x,210-height);e.Graphics.DrawString(r.Label,Font,Brushes.Black,new RectangleF(x,238,135,48));}}
}



