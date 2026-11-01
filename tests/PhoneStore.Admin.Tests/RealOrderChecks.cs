using PhoneStore.Admin.Client;

namespace PhoneStore.Admin.Tests;

internal static partial class Program
{
    private static async Task RealOrders()
    {
        var address=Environment.GetEnvironmentVariable("PHONESTORE_ADMIN_QA_URL")??throw new InvalidOperationException("QA URL missing");if(!ServerAddress.Validate(address).IsLoopback)throw new InvalidOperationException("QA loopback only");var signals=Environment.GetEnvironmentVariable("PHONESTORE_ADMIN_QA_SIGNAL_DIR")??throw new InvalidOperationException("SQL handshake missing");var ids=(Environment.GetEnvironmentVariable("PHONESTORE_ADMIN_QA_ORDER_IDS")??"").Split(',').Select(long.Parse).ToArray();Assert(ids.Length==3,"QA order IDs missing");
        async Task Signal(string stage){File.WriteAllText(Path.Combine(signals,stage+".ready"),"");for(var i=0;i<800;i++){if(File.Exists(Path.Combine(signals,stage+".continue")))return;await Task.Delay(50);}throw new InvalidOperationException("SQL handshake timed out "+stage);}
        using var api=new ApiClient(new(address,"Testing"));await api.LoginAsync(Environment.GetEnvironmentVariable("PHONESTORE_ADMIN_QA_USERNAME")!,Environment.GetEnvironmentVariable("PHONESTORE_ADMIN_QA_PASSWORD")!);
        using(var list=new OrderListForm(api)){list.Show();for(var i=0;i<100&&list.Grid.Rows.Count==0;i++)await Task.Delay(50);Assert(list.Grid.Rows.Count==3,"Real order list failed");await Capture(list,"UI02-real-order-list");list.Close();}
        string confirmationName="";using var approve=new System.Windows.Forms.Timer{Interval=80};approve.Tick+=async(_,_)=>{var dialog=Application.OpenForms.OfType<ConfirmationDialog>().FirstOrDefault();if(dialog is null)return;approve.Stop();if(confirmationName.Length>0)await Capture(dialog,confirmationName);dialog.ConfirmButton.PerformClick();};
        async Task Command(OrderDetailForm form,OrderAction action,string stage,string screenshot){approve.Start();await form.ActAsync(action);Assert(!form.MessageLabel.Text.Contains("Chưa thể")&&form.Current is not null,"Real order action failed: "+form.MessageLabel.Text);await Signal(stage);await Capture(form,screenshot);}
        using(var detail=new OrderDetailForm(api,ids[0]))
        {
            detail.Show();for(var i=0;i<100&&detail.Current is null;i++)await Task.Delay(50);Assert(detail.Current?.Status=="CHO_XAC_NHAN","Real waiting order failed");await Signal("opened");await Capture(detail,"UI11-real-CHO_XAC_NHAN");confirmationName="order-real-confirmation";await Command(detail,OrderAction.Confirm,"confirmed","UI11-real-DA_XAC_NHAN");confirmationName="";await Command(detail,OrderAction.Dispatch,"dispatched","UI11-real-DANG_GIAO");confirmationName="order-real-complete-COD-confirmation";await Command(detail,OrderAction.Complete,"completed","UI11-real-HOAN_TAT");Assert(detail.SummaryBox.Text.Contains("Đã thu: 24.000.000 đ"),"Real COD display wrong");detail.Controls.OfType<TableLayoutPanel>().Single().Controls.OfType<TabControl>().Single().SelectedIndex=1;await Capture(detail,"UI11-real-history-actors");detail.Close();
        }
        using(var invoice=new InvoiceForm(api,ids[0]))
        {
            invoice.Show();for(var i=0;i<100&&invoice.Current is null;i++)await Task.Delay(50);Assert(invoice.Current is not null&&invoice.NoticeLabel.Text.Contains("không phải hóa đơn điện tử thuế"),"Real invoice failed");await Signal("invoice-read");await Capture(invoice,"invoice-real-snapshot");Assert(new InvoicePrinter().HasInstalledPrinter,"Real preview proof requires installed driver; no-printer branch is separately tested on CI");
            Exception? captureError=null;using var closePreview=new System.Windows.Forms.Timer{Interval=200};closePreview.Tick+=async(_,_)=>{var preview=Application.OpenForms.OfType<PrintPreviewDialog>().FirstOrDefault();if(preview is null)return;closePreview.Stop();try{await Task.Delay(800);await Capture(preview,"PrintPreview-real-installed-driver");}catch(Exception error){captureError=error;}finally{preview.Close();}};closePreview.Start();invoice.Preview();Assert(captureError is null&&invoice.MessageLabel.Text.StartsWith("Đã đóng"),"Real PrintPreview failed");await Signal("preview-closed");invoice.Close();
        }
        using(var failed=new OrderDetailForm(api,ids[1]))
        {
            failed.Show();for(var i=0;i<100&&failed.Current is null;i++)await Task.Delay(50);failed.ReasonBox.Text="Khách không nhận hàng QA";confirmationName="order-real-failure-confirmation";await Command(failed,OrderAction.Fail,"failed","UI11-real-GIAO_THAT_BAI");failed.ReasonBox.Clear();confirmationName="order-real-return-physical-confirmation";await Command(failed,OrderAction.ReceiveReturn,"returned","UI11-real-return-received");Assert(failed.Current?.ReturnReceived==true&&!failed.ActionButtons[OrderAction.ReceiveReturn].Visible,"Real return flag/button wrong");failed.Close();
        }
        using(var cancel=new OrderDetailForm(api,ids[2]))
        {
            cancel.Show();for(var i=0;i<100&&cancel.Current is null;i++)await Task.Delay(50);cancel.ReasonBox.Text="Hủy đơn QA";confirmationName="order-real-cancel-confirmation";await Command(cancel,OrderAction.Cancel,"cancelled","UI11-real-DA_HUY");cancel.Close();
        }
        await api.LogoutAsync();File.WriteAllText(Path.Combine(output,"results.json"),System.Text.Json.JsonSerializer.Serialize(new{fakeApi=false,qaOnly=true,captures},new System.Text.Json.JsonSerializerOptions{WriteIndented=true}));passed++;Console.WriteLine("PASS W5 actual WinForms order transitions/invoice/installed-driver PrintPreview with SQL step barriers");
    }
}
