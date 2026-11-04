using System.Runtime.InteropServices;

namespace PhoneStore.Admin.Tests;

internal static partial class Program
{
    [StructLayout(LayoutKind.Sequential)]private struct NativeRect{public int Left,Top,Right,Bottom;}
    [DllImport("user32.dll")]private static extern IntPtr SetThreadDpiAwarenessContext(IntPtr value);
    [DllImport("user32.dll")]private static extern IntPtr SendMessage(IntPtr window,uint message,IntPtr param,ref NativeRect rect);
    private static async Task DpiCloseoutChecks()
    {
        var old=output;output=Path.Combine(FindRoot(),".artifacts","admin-w6","dpi");Directory.CreateDirectory(output);var evidence=new List<object>();var issues=new List<string>();
        using var basic=new FakeApi();using var catalog=new CatalogFake();using var contacts=new ContactsFake();using var supply=new SupplyFake();using var orders=new OrdersFake();using var invoice=new OrdersFake{Row=OrdersFake.Order("DA_XAC_NHAN")};using var reports=new ReportFake();
        var cases=new (string Name,Func<Form> Create)[]{("UI01-login",()=>new LoginForm(basic,new("https://qa.example.test"))),("UI02-dashboard",()=>new MainForm(reports,new("ready","QA fake","dpi-fixture"))),("UI08-products",()=>new ProductListForm(catalog)),("UI08-product-editor",()=>new ProductEditor(catalog,1)),("lookups-brands",()=>new LookupForm(catalog,"brands")),("lookups-categories",()=>new LookupForm(catalog,"categories")),("UI09-purchases",()=>new PurchaseListForm(supply)),("UI09-purchase-editor",()=>new PurchaseEditor(supply,1)),("UI10-stock",()=>new StockListForm(supply)),("UI10-count",()=>new StockCountDialog(supply,supply.Stocks[0])),("UI10-journal",()=>new StockJournalForm(supply,supply.Stocks[0])),("UI11-customers",()=>new CustomerListForm(contacts)),("UI11-customer-detail",()=>new CustomerDetailsForm(contacts,1)),("UI11-profile",()=>new CustomerProfileEditor(contacts,1)),("suppliers",()=>new SupplierListForm(contacts)),("supplier-editor",()=>new SupplierEditor(contacts)),("supplier-picker",()=>new SupplierPickerForm(contacts)),("UI11-orders",()=>new OrderListForm(orders)),("UI11-order-detail",()=>new OrderDetailForm(orders,33)),("invoice",()=>new InvoiceForm(invoice,33)),("UI12-reports",()=>new ReportScreen(reports)),("password",()=>new PasswordForm(basic)),("settings",()=>new ServerSettingsForm(new("https://qa.example.test"))),("confirmation",()=>new ConfirmationDialog("Xác nhận nhập hàng","Tổng tiền từ máy chủ: 125.000.000 đ. Lượng nhập: 15 · 2 dòng."))};
        foreach(var dpi in new[]{120,144})foreach(var test in cases)
        {
            using var form=test.Create();form.Show();await Task.Delay(100);var before=form.Font.SizeInPoints;var eventSeen=false;
            if(dpi==144){form.DpiChanged+=(_,e)=>eventSeen=e.DeviceDpiNew==144;var rect=new NativeRect{Left=20,Top=20,Right=1300,Bottom=820};SendMessage(form.Handle,0x02E0,new IntPtr((144<<16)|144),ref rect);await Task.Delay(80);Assert(eventSeen,"Native DPI event not delivered");}
            form.MinimumSize=new(1280,800);form.Size=new(1280,800);form.PerformLayout();await Task.Delay(70);
            var tabs=Descendants(form).OfType<TabControl>().ToArray();var pages=tabs.Length==0?1:tabs[0].TabPages.Count;
            for(var page=0;page<pages;page++)
            {
                if(tabs.Length>0)tabs[0].SelectedIndex=page;await Task.Delay(40);form.PerformLayout();var findings=new List<string>();
                foreach(var control in Descendants(form).Where(c=>c.Visible&&c.Parent is not null))
                {
                    if(control is VScrollBar or HScrollBar)continue;var parent=control.Parent!;var bounds=parent.ClientRectangle;
                    if(control.Left< -1||control.Top< -1||control.Right>bounds.Right+1||control.Bottom>bounds.Bottom+1)findings.Add($"bounds {control.GetType().Name} [{control.Text.Replace("\r","").Replace("\n"," ")}] {control.Bounds} parent={bounds}");
                    if(control is DateTimePicker date){var text=TextRenderer.MeasureText(date.Text,date.Font,Size.Empty,TextFormatFlags.NoPadding|TextFormatFlags.SingleLine);var chrome=(date.ShowCheckBox?date.Font.Height:0)+SystemInformation.VerticalScrollBarWidth+8;if(text.Width+chrome>date.ClientSize.Width)findings.Add($"date text {date.AccessibleName} [{date.Text}] measured={text.Width} chrome={chrome} width={date.ClientSize.Width}");}
                    if(control is Label label&&label.Text.Length>0&&!label.AutoEllipsis)
                    {var size=TextRenderer.MeasureText(label.Text,label.Font,new Size(Math.Max(1,label.ClientSize.Width),int.MaxValue),TextFormatFlags.WordBreak|TextFormatFlags.TextBoxControl);if(size.Height>label.ClientSize.Height+3)findings.Add($"text Label [{label.Text}] measured={size} client={label.ClientSize}");}
                    if(control is Button button&&button.Text.Length>0){var size=TextRenderer.MeasureText(button.Text.Replace("&",""),button.Font);if(size.Height>button.ClientSize.Height+1||size.Width>button.ClientSize.Width+1)findings.Add($"text Button [{button.Text}] measured={size} client={button.ClientSize}");}
                    if(control.TabStop&&control.Parent is not NumericUpDown&&control is TextBox or ComboBox or NumericUpDown or DateTimePicker)Assert(!string.IsNullOrWhiteSpace(control.AccessibleName),"Missing input label "+test.Name+"/"+control.GetType().Name);
                }
                var name=test.Name+"-"+(dpi==120?((form.DeviceDpi*100/96)+"-real"):"150-native")+"-tab"+page;await Capture(form,name);evidence.Add(new{name,actualDeviceDpi=form.DeviceDpi,nativeEventDpi=dpi==144?144:(int?)null,fontBefore=before,fontAfter=form.Font.SizeInPoints,form.Width,form.Height,findings});issues.AddRange(findings.Select(x=>name+": "+x));
            }
            if(form is MainForm main)main.EndSession();else UnsavedChanges.DiscardAndClose(form);
        }
        File.WriteAllText(Path.Combine(output,"results.json"),System.Text.Json.JsonSerializer.Serialize(new{method="Actual desktop baseline (local120DPI=125%; CI may96DPI=100%);150% simulated WM_DPICHANGED native event; no bitmap resize; minimum1280x800",fakeApi=true,evidence,issues},new System.Text.Json.JsonSerializerOptions{WriteIndented=true}));output=old;Assert(issues.Count==0,$"DPI layout/text issues: {issues.Count}; see results.json");Console.WriteLine("PASS WIN.UI.01/02/03 native DPI/layout/labels "+evidence.Count+" captures");
    }
    private static IEnumerable<Control> Descendants(Control parent){foreach(Control child in parent.Controls){yield return child;foreach(var item in Descendants(child))yield return item;}}
}
