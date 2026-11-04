using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using PhoneStore.Admin.Client;

namespace PhoneStore.Admin.Tests;

internal static partial class Program
{
    // Explicit, read-only acceptance mode. Uses the existing STA message loop and
    // shipping forms with the real ApiClient; no fake API and no keyboard input.
    private static async Task DemoCloseoutSta()
    {
        var address=Environment.GetEnvironmentVariable("PHONESTORE_ADMIN_QA_URL")??throw new InvalidOperationException("Loopback host required");
        Assert(ServerAddress.Validate(address).IsLoopback,"Loopback host required");
        using var context=JsonDocument.Parse(File.ReadAllText(Environment.GetEnvironmentVariable("PHONESTORE_DEMO_CONTEXT")??throw new InvalidOperationException("Demo context required")));
        var c=context.RootElement;var database=c.GetProperty("database").GetString()!;
        Assert(database.StartsWith("PhoneStore_Demo_",StringComparison.Ordinal),"Dedicated demo required");
        var observations=new List<object>();var timings=new List<object>();var stage="login";var success=false;
        using var api=new ApiClient(new(address,"Testing"));
        async Task Loaded(Func<bool> condition){for(var i=0;i<200&&!condition();i++)await Task.Delay(50);Assert(condition(),"Form did not load: "+stage);}
        async Task Observe(Form form,string name){await Capture(form,name);observations.Add(new{screen=stage,name,realApi=true,shippingForm=true});}
        void RealNames(IEnumerable<string> names){var rows=names.ToArray();Assert(rows.Length>0&&rows.All(n=>!n.Contains("Auvora",StringComparison.OrdinalIgnoreCase)&&!n.Contains("Lumetrix",StringComparison.OrdinalIgnoreCase)&&!n.Contains("DM001",StringComparison.OrdinalIgnoreCase)),"Legacy/empty product labels");}
        try
        {
            await api.LoginAsync(Environment.GetEnvironmentVariable("PHONESTORE_ADMIN_QA_USERNAME")!,Environment.GetEnvironmentVariable("PHONESTORE_ADMIN_QA_PASSWORD")!);
            stage="dashboard";
            using(var form=new Form{Text="Demo closeout dashboard",Size=new(1280,800)})
            using(var dashboard=new DashboardView(api))
            {
                form.Controls.Add(dashboard);form.Show();dashboard.ThresholdBox.Value=5;await dashboard.LoadAsync();
                Assert(dashboard.MetricLabels["period"].Text.Contains(AdminFormatting.Money(c.GetProperty("expected").GetProperty("periodRevenue").GetDecimal())),"Dashboard revenue differs from SQL context");
                Assert(dashboard.Chart.Points.Count==7,"Dashboard chart missing");await Observe(form,"sta-dashboard");form.Close();
            }
            stage="orders";
            using(var form=new OrderListForm(api))
            {
                var timer=Stopwatch.StartNew();form.Show();await Loaded(()=>form.Grid.Rows.Count>0);Assert(form.MessageLabel.Text.Contains("601 đơn"),"Full order count differs");
                timings.Add(new{screen=stage,milliseconds=timer.ElapsedMilliseconds});await Observe(form,"sta-orders-page1");form.NextButton.PerformClick();await Loaded(()=>form.MessageLabel.Text.Contains("Trang 2"));await Observe(form,"sta-orders-page2");form.Close();
            }
            stage="old-order-details";
            foreach(var old in c.GetProperty("historicalInvoices").EnumerateArray())
            {
                using var form=new OrderDetailForm(api,old.GetProperty("id").GetInt64());form.Show();await Loaded(()=>form.Current is not null);
                Assert(form.Current!.Code==old.GetProperty("code").GetString()&&form.HistoryGrid.Rows.Count>0,"Old detail/history differs");RealNames(form.Current.Lines.Select(x=>x.Name));await Observe(form,"sta-old-order-"+old.GetProperty("id").GetInt64());form.Close();
            }
            stage="invoices";var previewed=false;
            foreach(var old in c.GetProperty("historicalInvoices").EnumerateArray())
            {
                using var form=new InvoiceForm(api,old.GetProperty("id").GetInt64());form.Show();await Loaded(()=>form.Current is not null);
                Assert(form.Current!.Order.Total==old.GetProperty("total").GetDecimal()&&form.ContentBox.Text.Contains(old.GetProperty("name").GetString()!),"Invoice snapshot differs");await Observe(form,"sta-invoice-"+old.GetProperty("id").GetInt64());
                if(!previewed)
                {
                    using var timer=new System.Windows.Forms.Timer{Interval=200};Exception? previewError=null;
                    timer.Tick+=async(_,_)=>{var preview=Application.OpenForms.OfType<PrintPreviewDialog>().FirstOrDefault();if(preview is null)return;timer.Stop();try{await Observe(preview,"sta-invoice-print-preview");previewed=true;}catch(Exception e){previewError=e;}finally{preview.Close();}};
                    timer.Start();form.PreviewButton.PerformClick();timer.Stop();if(previewError is not null)throw previewError;Assert(previewed,"Actual PrintPreview unavailable");
                }
                form.Close();
            }
            stage="old-purchases";
            foreach(var old in c.GetProperty("historicalPurchases").EnumerateArray())
            {
                using var form=new PurchaseEditor(api,old.GetProperty("id").GetInt64());form.Show();await Loaded(()=>form.Current is not null);
                Assert(form.Current!.Total==old.GetProperty("total").GetDecimal()&&form.LinesGrid.Rows.Count>0,"Old receipt differs");RealNames(form.Current.Lines.Select(x=>x.Name));await Observe(form,"sta-purchase-"+old.GetProperty("id").GetInt64());form.Close();
            }
            stage="stock";
            using(var form=new StockListForm(api,5,initialLow:true))
            {
                form.Show();await Loaded(()=>form.Grid.Rows.Count>0);Assert(form.Grid.Rows.Cast<DataGridViewRow>().Any(r=>r.Tag is StockRecord s&&s.OnHand==0),"Out-of-stock row missing");await Observe(form,"sta-stock-low-and-zero");form.Close();
            }
            stage="quarter-reports";
            using(var form=new ReportScreen(api))
            {
                form.Show();await Loaded(()=>form.Current is not null);var index=0;
                foreach(var quarter in c.GetProperty("quarters").EnumerateArray())
                {
                    form.FromBox.Checked=form.ToBox.Checked=true;
                    form.FromBox.Value=DateTime.ParseExact(quarter.GetProperty("from").GetString()!,"yyyy-MM-dd",CultureInfo.InvariantCulture);
                    form.ToBox.Value=DateTime.ParseExact(quarter.GetProperty("to").GetString()!,"yyyy-MM-dd",CultureInfo.InvariantCulture);
                    var timer=Stopwatch.StartNew();await form.LoadAsync();
                    Assert(form.Current is not null&&form.Current.Revenue==quarter.GetProperty("revenue").GetDecimal()&&form.Current.Revenue>0,"Quarter report differs from SQL context");
                    Assert(form.SummaryLabel.Text.Contains(AdminFormatting.Money(form.Current!.Revenue))&&form.Grid.Rows.Count>0,"Quarter not rendered");timings.Add(new{screen=stage,quarter=index,milliseconds=timer.ElapsedMilliseconds});await Observe(form,"sta-quarter-"+index++);
                }
                form.KindBox.SelectedIndex=1;await form.LoadAsync();Assert(form.Current!.Bestsellers.Count>0,"Bestsellers empty");RealNames(form.Current.Bestsellers.Select(x=>x.Name));await Observe(form,"sta-bestsellers");
                form.KindBox.SelectedIndex=2;await form.LoadAsync();Assert(form.Current!.Stock.Count==125,"Stock report count differs");RealNames(form.Current.Stock.Select(x=>x.Name));await Observe(form,"sta-stock-report");form.Close();
            }
            success=true;Console.WriteLine("PASS one STA demo acceptance attempt: real API, programmatic dates, SQL-matched quarters and historical forms");
        }
        finally
        {
            if(api.Identity is not null)await api.LogoutAsync();
            File.WriteAllText(Path.Combine(output,"demo-sta-results.json"),JsonSerializer.Serialize(new{result=success?"PASS":"NOT RUN",attempts=1,realApi=true,fakeApi=false,sta=true,keyboardInput=false,programmaticDateTimePicker=true,stage,observations,timings,captures,manualNote=success?null:"chờ kiểm tra thủ công bởi người dùng"},new JsonSerializerOptions{WriteIndented=true}));
        }
    }
}
