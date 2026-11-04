using PhoneStore.Admin.Client;

namespace PhoneStore.Admin.Tests;

internal static partial class Program
{
    private static async Task RealSupply()
    {
        var address=Environment.GetEnvironmentVariable("PHONESTORE_ADMIN_QA_URL")??throw new InvalidOperationException("QA URL missing");if(!ServerAddress.Validate(address).IsLoopback)throw new InvalidOperationException("QA loopback only");
        var signals=Environment.GetEnvironmentVariable("PHONESTORE_ADMIN_QA_SIGNAL_DIR")??throw new InvalidOperationException("QA SQL handshake missing");
        async Task Signal(string stage){File.WriteAllText(Path.Combine(signals,stage+".ready"),"");for(var i=0;i<600;i++){if(File.Exists(Path.Combine(signals,stage+".continue")))return;await Task.Delay(50);}throw new InvalidOperationException("QA SQL handshake timed out: "+stage);}
        using var api=new ApiClient(new(address,"Testing"));await api.LoginAsync(Environment.GetEnvironmentVariable("PHONESTORE_ADMIN_QA_USERNAME")!,Environment.GetEnvironmentVariable("PHONESTORE_ADMIN_QA_PASSWORD")!);
        var catalog=new CatalogWorkspace(api);await catalog.SaveLookup("brands",0,new("Hãng nhập kho QA",true));await catalog.SaveLookup("categories",0,new("Điện thoại QA",true));await new ContactsWorkspace(api).SaveSupplier(0,new("SUP-W4-UI","NCC nhập hàng QA","0938123593","QA only",true));
        Exception? modalFailure=null;using(var product=new ProductEditor(api))
        {
            product.Show();for(var i=0;i<100&&product.BrandBox.Items.Count==0;i++)await Task.Delay(50);product.CodeBox.Text="QA-W4-UI";product.NameBox.Text="Điện thoại nhập kho · API thực";product.Variants[0].Sku="QA-W4-UI-A";product.Variants[0].Color="Đen";product.Variants[0].Cost=10000000;product.Variants[0].SuggestedQuantity=4;await product.SaveAsync();Assert(product.Saved is not null&&product.Variants[0].SuggestedQuantity==4&&product.PurchaseButton.Enabled,"Product hints lost or handoff disabled");await Signal("product-saved");product.Controls.OfType<TableLayoutPanel>().Single().Controls.OfType<TabControl>().Single().SelectedIndex=1;product.VariantGrid.FirstDisplayedScrollingColumnIndex=5;await Capture(product,"UI08-real-product-hints");
            using var approve=new System.Windows.Forms.Timer{Interval=100};approve.Tick+=async(_,_)=>{var dialog=Application.OpenForms.OfType<ConfirmationDialog>().FirstOrDefault();if(dialog is null)return;approve.Stop();await Capture(dialog,"UI09-real-confirm-dialog");dialog.ConfirmButton.PerformClick();};
            using var timer=new System.Windows.Forms.Timer{Interval=100};timer.Tick+=async(_,_)=>
            {
                var receipt=Application.OpenForms.OfType<PurchaseEditor>().FirstOrDefault();if(receipt is null||receipt.Lines.Count==0||!receipt.SaveButton.Enabled)return;timer.Stop();
                try
                {
                    Assert(receipt.Lines.Single().Quantity==4,"Handoff quantity wrong");await Signal("handoff-open");await Capture(receipt,"UI09-real-handoff-unsaved");await receipt.SaveAsync();Assert(receipt.Current is{Status:"NHAP",Total:40000000},"Real draft save failed: "+receipt.MessageLabel.Text);await Signal("draft-saved");await Capture(receipt,"UI09-real-purchase-draft");approve.Start();await receipt.ActAsync("confirm");Assert(receipt.Current is{Status:"DA_XAC_NHAN"}&&receipt.ImpactGrid.Rows.Count==1,"Real confirm failed: "+receipt.MessageLabel.Text);await Signal("confirmed");var tabs=receipt.Controls.OfType<TableLayoutPanel>().Single().Controls.OfType<TabControl>().Single();tabs.SelectedIndex=1;await Capture(receipt,"UI09-real-confirmed-stock-price");
                }
                catch(Exception e){modalFailure=e;}finally{approve.Stop();receipt.Close();}
            };
            timer.Start();product.PurchaseButton.PerformClick();Assert(modalFailure is null,"Real handoff failed: "+modalFailure?.Message);product.Close();
        }
        var supply=new SupplyWorkspace(api);var stock=(await supply.Stock(null,null,null)).Items.Single();using(var list=new StockListForm(api)){list.Show();for(var i=0;i<100&&list.Grid.Rows.Count==0;i++)await Task.Delay(50);await Capture(list,"UI10-real-stock-list");list.Close();}
        using(var count=new StockCountDialog(api,stock))
        {
            count.Show();count.CountedBox.Value=5;count.ReasonBox.Text="Kiểm kê QA thực";await Capture(count,"UI10-real-count-input");using var approve=new System.Windows.Forms.Timer{Interval=100};approve.Tick+=async(_,_)=>{var dialog=Application.OpenForms.OfType<ConfirmationDialog>().FirstOrDefault();if(dialog is null)return;approve.Stop();await Capture(dialog,"UI10-real-count-confirm");dialog.ConfirmButton.PerformClick();};approve.Start();await count.SubmitAsync();Assert(!count.SubmitButton.Enabled&&count.MessageLabel.Text.StartsWith("Đã ghi nhận"),"Real count failed: "+count.MessageLabel.Text);await Signal("counted");await Capture(count,"UI10-real-count-saved");count.Close();
        }
        using(var journal=new StockJournalForm(api,stock)){journal.Show();await Task.Delay(150);await Capture(journal,"UI10-real-journal");journal.Close();}
        await api.LogoutAsync();File.WriteAllText(Path.Combine(output,"results.json"),System.Text.Json.JsonSerializer.Serialize(new{fakeApi=false,qaOnly=true,captures},new System.Text.Json.JsonSerializerOptions{WriteIndented=true}));Console.WriteLine("PASS WIN.A05.04/A14/A15/A17 actual product-to-purchase/confirm/count with parent SQL barrier per step");passed++;
    }
}
