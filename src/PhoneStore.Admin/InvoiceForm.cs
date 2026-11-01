using System.Drawing.Printing;
using PhoneStore.Admin.Client;

namespace PhoneStore.Admin;

public interface IInvoicePrinter
{
    bool HasInstalledPrinter { get; }
    void Preview(IWin32Window owner,AdminInvoice invoice);
}
public sealed class InvoicePrinter:IInvoicePrinter
{
    public bool HasInstalledPrinter=>PrinterSettings.InstalledPrinters.Count>0;
    public void Preview(IWin32Window owner,AdminInvoice invoice)
    {
        using var document=new PrintDocument{DocumentName=invoice.Order.Code};using var font=new Font("Segoe UI",10);
        var remaining="";
        document.BeginPrint+=(_,_)=>remaining=OrderPresentation.InvoiceText(invoice);
        document.PrintPage+=(_,args)=>
        {
            if(args.Graphics is null){args.Cancel=true;return;}
            var box=args.MarginBounds;args.Graphics.MeasureString(remaining,font,box.Size,StringFormat.GenericTypographic,out var count,out var lines);
            if(count==0){args.Cancel=true;return;}
            args.Graphics.DrawString(remaining[..count],font,Brushes.Black,box,StringFormat.GenericTypographic);remaining=remaining[count..];args.HasMorePages=remaining.Length>0;
        };
        using var preview=new PrintPreviewDialog{Document=document,Text="Xem trước in · "+invoice.Order.Code,Size=new(1280,800),MinimumSize=new(800,600),StartPosition=FormStartPosition.CenterParent};
        preview.ShowDialog(owner);
    }
}
public sealed class InvoiceForm:Form
{
    private readonly OrdersWorkspace workspace;private readonly long id;private readonly IInvoicePrinter printer;private bool busy;
    public AdminInvoice? Current { get; private set; }
    public Label TitleLabel { get; }=AdminTheme.Label("Hóa đơn bán hàng",22,true);public Label NoticeLabel { get; }=AdminTheme.Label("");public Label MessageLabel { get; }=AdminTheme.Label("");
    public TextBox ContentBox { get; }=new(){Multiline=true,ReadOnly=true,Dock=DockStyle.Fill,ScrollBars=ScrollBars.Both,AccessibleName="Nội dung hóa đơn snapshot"};
    public Button PreviewButton { get; }=AdminTheme.Button("PrintPreview · Xem trước in");public Button ReloadButton { get; }=AdminTheme.Button("Tải lại hóa đơn",false);
    public InvoiceForm(IAdminApi api,long orderId,IInvoicePrinter? printer=null)
    {
        workspace=new(api);id=orderId;this.printer=printer??new InvoicePrinter();AdminTheme.Frame(this,"Hóa đơn bán hàng");
        var root=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=5,Padding=new Padding(24)};root.RowStyles.Add(new RowStyle(SizeType.AutoSize));root.RowStyles.Add(new RowStyle(SizeType.AutoSize));root.RowStyles.Add(new RowStyle(SizeType.Percent,100));root.RowStyles.Add(new RowStyle(SizeType.AutoSize));root.RowStyles.Add(new RowStyle(SizeType.AutoSize));NoticeLabel.MaximumSize=new Size(1150,0);root.Controls.Add(TitleLabel,0,0);root.Controls.Add(NoticeLabel,0,1);root.Controls.Add(ContentBox,0,2);root.Controls.Add(MessageLabel,0,3);root.Controls.Add(CatalogUi.Toolbar(PreviewButton,ReloadButton),0,4);Controls.Add(root);
        PreviewButton.Click+=(_,_)=>Preview();ReloadButton.Click+=async(_,_)=>await LoadAsync();Shown+=async(_,_)=>await LoadAsync();Actions();
    }
    private void Actions(){PreviewButton.Enabled=!busy&&Current is not null;ReloadButton.Enabled=!busy;}
    public async Task LoadAsync()
    {
        if(busy)return;busy=true;Actions();MessageLabel.Text="Đang tải hóa đơn…";
        try{Current=await workspace.Invoice(id);TitleLabel.Text=Current.Title;NoticeLabel.Text=Current.Notice;ContentBox.Text=OrderPresentation.InvoiceText(Current);MessageLabel.Text="Snapshot từ máy chủ. Xem và in không thay đổi đơn hàng hoặc COD.";}
        catch(Exception error){Current=null;NoticeLabel.Text="";ContentBox.Clear();MessageLabel.Text=AdminTheme.Failure(error);}finally{busy=false;Actions();}
    }
    public void Preview()
    {
        if(busy||Current is null)return;busy=true;Actions();
        try{if(!printer.HasInstalledPrinter){MessageLabel.Text="Chưa có máy in hoặc driver nào được cài. Bạn vẫn có thể xem nội dung hóa đơn tại đây; cài máy in để dùng PrintPreview.";return;}printer.Preview(this,Current);MessageLabel.Text="Đã đóng xem trước. Không thay đổi đơn hàng hoặc COD.";}
        catch(Exception){MessageLabel.Text="Chưa mở được PrintPreview. Kiểm tra máy in hoặc driver; nội dung hóa đơn vẫn được giữ nguyên.";}
        finally{busy=false;Actions();}
    }
}
