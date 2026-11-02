using PhoneStore.Admin.Client;

namespace PhoneStore.Admin.Tests;

internal static partial class Program
{
    private static async Task BaseCloseoutChecks()
    {
        await Test("WIN.BASE.15 every destructive confirmation can dismiss with zero writes",async()=>
        {
            using var catalog=new CatalogFake();using(var form=new ProductListForm(catalog,(_,_)=>false)){form.Show();await Task.Delay(70);await form.ChangeStateAsync();Assert(catalog.Writes==0,"Product dismissal wrote");form.Close();}
            foreach(var kind in new[]{"brands","categories"})using(var form=new LookupForm(catalog,kind,(_,_)=>false)){form.Show();await Task.Delay(60);form.ActiveBox.Checked=!form.ActiveBox.Checked;await form.SaveAsync();Assert(catalog.Writes==0,"Lookup dismissal wrote");form.Close();}
            foreach(var active in new[]{true,false}){using var api=new ContactsFake();api.Customer=api.Customer with{Active=active};using var form=new CustomerDetailsForm(api,1,(_,_)=>false);form.Show();await Task.Delay(60);await form.ChangeStateAsync();Assert(api.Writes==0,"Customer lock/unlock dismissal wrote");form.Close();}
            using var supply=new SupplyFake();using(var form=new PurchaseEditor(supply,1,confirmation:(_,_)=>false)){form.Show();await Task.Delay(60);await form.ActAsync("confirm");Assert(supply.Writes==0,"Purchase dismissal wrote");form.Close();}
            using(var form=new StockCountDialog(supply,supply.Stocks[0],(_,_)=>false)){form.Show();form.CountedBox.Value=supply.Stocks[0].OnHand+2;form.ReasonBox.Text="Lý do QA";await form.SubmitAsync();Assert(supply.Writes==0,"Count dismissal wrote");form.Close();}
            foreach(var(state,action)in new[]{("CHO_XAC_NHAN",OrderAction.Cancel),("DANG_GIAO",OrderAction.Fail),("GIAO_THAT_BAI",OrderAction.ReceiveReturn)}){using var api=new OrdersFake{Row=OrdersFake.Order(state)};using var form=new OrderDetailForm(api,33,(_,_)=>false);form.Show();await Task.Delay(60);form.ReasonBox.Text="Giữ nguyên lý do QA";await form.ActAsync(action);Assert(api.Writes==0&&form.ReasonBox.Text=="Giữ nguyên lý do QA","Order dismissal wrote or lost fields");form.Close();}
        });
        await Test("WIN.UI.03 native Tab/Enter/Escape/CtrlS; safe confirmation default; clear input names",async()=>
        {
            using var api=new FakeApi();using(var login=new LoginForm(api,new("https://qa.example.test"))){login.Show();await Task.Delay(60);login.UsernameBox.Focus();typeof(Form).GetMethod("ProcessDialogKey",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!.Invoke(login,[Keys.Tab]);Assert(login.PasswordBox.Focused,"Tab skipped password");Assert(login.AcceptButton==login.SubmitButton&&login.CancelButton is not null,"Login default/cancel absent");login.Close();}
            using var catalog=new CatalogFake();using(var form=new ProductEditor(catalog,1)){form.Show();await Task.Delay(70);Assert(form.AcceptButton==form.SaveButton,"Editor default save absent");typeof(Control).GetMethod("OnKeyDown",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!.Invoke(form,[new KeyEventArgs(Keys.Control|Keys.S)]);await Task.Delay(60);Assert(catalog.Writes==1,"CtrlS did not save exactly once");form.Close();}
            using var contacts=new ContactsFake();using(var form=new SupplierEditor(contacts)){using var timer=new System.Windows.Forms.Timer{Interval=60};timer.Tick+=(_,_)=>{timer.Stop();typeof(Control).GetMethod("OnKeyDown",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!.Invoke(form,[new KeyEventArgs(Keys.Escape)]);};timer.Start();form.ShowDialog();Assert(contacts.Writes==0,"Escape wrote data");}
        });
        await Test("WIN.BASE.13/14/16 W6 native navigation/report hold-load and retained date controls",async()=>
        {
            using var api=new ReportFake();using var main=new MainForm(api,await api.ReadinessAsync());main.Show();await Task.Delay(70);Assert(main.Navigation.All(b=>b.Enabled)&&!Descendants(main).Any(c=>c.Text=="Chưa triển khai"),"Unimplemented navigation remains");
            using var timer=new System.Windows.Forms.Timer{Interval=60};var opened=false;timer.Tick+=(_,_)=>{var report=Application.OpenForms.OfType<ReportScreen>().FirstOrDefault();if(report?.Current is null)return;opened=true;timer.Stop();report.Close();};timer.Start();main.Navigation.Single(b=>b.Text=="Báo cáo").PerformClick();Assert(opened,"Report navigation absent");main.EndSession();
            using var form=new ReportScreen(api);form.Show();await Task.Delay(60);var from=form.FromBox.Value;var to=form.ToBox.Value;api.ReadError=new AdminApiException("DATABASE_UNAVAILABLE",503);await form.LoadAsync();Assert(form.FromBox.Value==from&&form.ToBox.Value==to&&form.LoadButton.Enabled&&form.Current is null,"Report error lost filters or kept false totals");form.Close();
        });
    }
}
