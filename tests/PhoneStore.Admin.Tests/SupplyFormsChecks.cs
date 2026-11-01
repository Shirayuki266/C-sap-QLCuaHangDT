using System.Net;
using PhoneStore.Admin.Client;

namespace PhoneStore.Admin.Tests;

internal static partial class Program
{
    private static async Task SupplyFormsChecks()
    {
        var previous=output;output=Path.Combine(FindRoot(),".artifacts","admin-w4","forms");Directory.CreateDirectory(output);var start=passed;var captureOffset=captures.Count;
        await Test("WIN.A14.01/03/04/05 actual purchase list/editor server totals, empty draft, duplicate validation and final-state buttons",async()=>
        {
            using var api=new SupplyFake();using(var list=new PurchaseListForm(api)){list.Show();await Task.Delay(50);Assert(list.Grid.Rows.Count==1,"Purchase list absent");await Capture(list,"UI09-purchase-list");list.Close();}
            using var editor=new PurchaseEditor(api,1,confirmation:(_,_)=>true);editor.Show();await Task.Delay(60);Assert(editor.Current is{Status:"NHAP"}&&editor.ConfirmButton.Enabled,"Draft actions absent");await Capture(editor,"UI09-purchase-editor");
            editor.Lines.Add(new(){VariantId=3,Sku="W4-A",Name="Duplicate",Quantity=1,Cost=1});var writes=api.Writes;await editor.SaveAsync();Assert(api.Writes==writes&&editor.MessageLabel.Text.Contains("không trùng"),"Duplicate line sent");editor.Lines.RemoveAt(editor.Lines.Count-1);
            api.WriteError=new AdminApiException("INVALID_INPUT",400,null,"Giá vốn nhập không hợp lệ.");editor.Lines[0].Quantity=8;await editor.SaveAsync();Assert(editor.Lines[0].Quantity==8&&editor.MessageLabel.Text.Contains("Giá vốn"),"Server failure lost form/public reason");await Capture(editor,"UI09-purchase-error-retained");api.WriteError=null;
            editor.Lines.Clear();await editor.SaveAsync();Assert(editor.Current is{Lines.Count:0}&&!editor.ConfirmButton.Enabled&&editor.CancelDraftButton.Enabled&&editor.ConfirmHintLabel.Text.Contains("Phiếu rỗng"),"Empty draft not supported");await Capture(editor,"UI09-empty-draft");await editor.ActAsync("cancel");Assert(editor.Current is{Status:"DA_HUY"}&&!editor.SaveButton.Enabled&&!editor.CancelDraftButton.Enabled,"Canceled draft still editable");editor.Close();
        });
        await Test("WIN.A15.01/02/03 confirmation gates show server total/quantity, key+version, busy guard and server impact readback",async()=>
        {
            using var api=new SupplyFake();var allow=false;var prompts=new List<string>();using var editor=new PurchaseEditor(api,1,confirmation:(t,m)=>{prompts.Add(m);return allow;});editor.Show();await Task.Delay(50);await editor.ActAsync("confirm");Assert(api.Writes==0,"Canceled confirmation sent command");
            allow=true;api.Hold=new(TaskCreationOptions.RunContinuationsAsynchronously);var pending=editor.ActAsync("confirm");await Task.Delay(30);editor.ConfirmButton.PerformClick();Assert(api.Writes==1&&!editor.ConfirmButton.Enabled&&editor.MessageLabel.Text.Contains("Đang"),"Duplicate/busy guard absent");await Capture(editor,"UI09-confirm-busy");api.Hold.SetResult();await pending;
            Assert(editor.Current is{Status:"DA_XAC_NHAN"}&&editor.ImpactGrid.Rows.Count==2&&!editor.SaveButton.Enabled&&api.LastTransition is{OperationKey:var key}&&key!=Guid.Empty,"Confirm/readback/key wrong");Assert(prompts.Any(p=>p.Contains("125.000.000 đ")&&p.Contains("15")&&p.Contains("2 dòng")),"Confirmation omitted total/quantity");editor.Controls.OfType<TableLayoutPanel>().Single().Controls.OfType<TabControl>().Single().SelectedIndex=1;await Capture(editor,"UI09-confirmed-server-balances");editor.Close();
            using var dialog=new ConfirmationDialog("Xác nhận nhập hàng",prompts.Last());dialog.Show();await Capture(dialog,"UI09-confirm-dialog");dialog.DismissButton.PerformClick();
        });
        await Test("WIN.A16.01/02/03 read-only stock/filter/explicit threshold, no-match versus failure, movement journal",async()=>
        {
            using var api=new SupplyFake();var savedThreshold=-1;using(var stock=new StockListForm(api,5,value=>savedThreshold=value)){stock.Show();await Task.Delay(50);Assert(stock.Grid.ReadOnly&&stock.Grid.Rows.Count==2&&!api.Paths.Last(p=>p.Contains("/stock?")).Contains("below="),"Hidden default threshold or editable grid");await Capture(stock,"UI10-stock-list");stock.BelowBox.Checked=true;stock.ThresholdBox.Value=8;stock.SearchButton.PerformClick();await Task.Delay(30);Assert(savedThreshold==8&&api.Paths.Any(p=>p.Contains("below=8")),"Explicit filter/preference absent");stock.QueryBox.Text="no-match";await stock.LoadAsync();Assert(stock.Grid.Rows.Count==0&&stock.MessageLabel.Text.Contains("Không có"),"No-match not empty");await Capture(stock,"UI10-stock-empty");api.StockError=new AdminApiException("DATABASE_UNAVAILABLE",503);await stock.LoadAsync();Assert(stock.Grid.Rows.Count==0&&stock.MessageLabel.Text.Contains("chưa sẵn sàng")&&!stock.MessageLabel.Text.Contains("0 biến thể"),"Read failure became zero stock");await Capture(stock,"UI10-stock-read-error");stock.Close();api.StockError=null;}
            using(var journal=new StockJournalForm(api,api.Stocks[0])){journal.Show();await Task.Delay(50);Assert(journal.Grid.Rows.Count>0,"Journal empty");await Capture(journal,"UI10-movement-journal");journal.Close();}
            var store=new StockPreferenceStore(Path.Combine(output,"preferences-qa"));store.Save(7);Assert(store.Load()==7,"User threshold did not persist");
        });
        await Test("WIN.A17.01/02/03/04 actual absolute-count dialog confirms delta/stock-version and reconciles lost response by key",async()=>
        {
            using var api=new SupplyFake();var prompts=new List<string>();using var form=new StockCountDialog(api,api.Stocks[0],(t,m)=>{prompts.Add(m);return true;});form.Show();form.CountedBox.Value=12;await form.SubmitAsync();Assert(api.Writes==0&&form.MessageLabel.Text.Contains("lý do"),"Reason not required");form.ReasonBox.Text="Kiểm kê QA";form.CountedBox.Value=1;await form.SubmitAsync();Assert(api.Writes==0&&form.MessageLabel.Text.Contains("đang giữ"),"Below reserved sent");
            form.CountedBox.Value=12;api.WriteError=new AdminApiException("CONCURRENT_UPDATE",409);await form.SubmitAsync();Assert(form.MessageLabel.Text.Contains("vui lòng tải lại")&&form.ReasonBox.Text=="Kiểm kê QA","Stale message/draft retention wrong");api.WriteError=null;api.LoseCommand=true;await form.SubmitAsync();var key=form.OperationKey;Assert(!form.SubmitButton.Enabled&&api.LastAdjustment?.Version==SupplyFake.StockVersion&&api.LastPath=="/api/admin/stock/3/adjust","Wrong version/productId or unknown retry enabled");
            await form.ResolveAsync();Assert(api.Stocks[0].OnHand==12&&!form.SubmitButton.Enabled&&api.Paths.Any(p=>p.Contains("operationKey="+key))&&api.Writes==2,"Lost response retried instead of journal reconcile");Assert(prompts.Any(p=>p.Contains("hiện có 10")&&p.Contains("số thực tế 12")&&p.Contains("+2")),"Count confirmation lacked absolute/delta");await Capture(form,"UI10-count-reconciled");form.Close();
            using(var fresh=new StockCountDialog(api,api.Stocks[0],(_,_)=>true)){fresh.Show();fresh.CountedBox.Value=13;fresh.ReasonBox.Text="Ý định mới QA";await Capture(fresh,"UI10-count-dialog");await fresh.SubmitAsync();Assert(fresh.OperationKey!=key,"New count reused key");fresh.Close();}
            using var dialog=new ConfirmationDialog("Xác nhận kiểm kê",prompts.Last());dialog.Show();await Capture(dialog,"UI10-count-confirm-dialog");dialog.DismissButton.PerformClick();
        });
        await Test("WIN.A14.01 uncertain create retry keeps same requestKey and payload, no duplicate draft",async()=>
        {
            using var api=new SupplyFake();using var editor=new PurchaseEditor(api);editor.Show();await Task.Delay(50);editor.Lines.Add(new(){VariantId=3,Sku="W4-A",Name="Điện thoại QA",Quantity=2,Cost=8000000});api.LoseCreate=true;var key=editor.RequestKey;await editor.SaveAsync();Assert(!editor.SaveButton.Enabled&&api.Purchases.Count==2,"Uncertain draft not guarded");await editor.LoadAsync();Assert(editor.Current is not null&&editor.RequestKey==key&&api.Purchases.Count==2&&api.LastCreation?.RequestKey==key,"Same-key retry changed intent/duplicated draft");editor.Close();
        });
        await Test("WIN.A05.04 ProductEditor default handoff opens prefilled purchase, no write until explicit Save",async()=>
        {
            using var api=new SupplyFake();using var product=new ProductEditor(api,2,(_,_)=>true);product.Show();await Task.Delay(50);product.Variants[0].SuggestedQuantity=7;Assert(product.PurchaseButton.Enabled,"Saved product handoff disabled");
            using var timer=new System.Windows.Forms.Timer{Interval=100};var clean=false;var saved=false;Exception? failure=null;timer.Tick+=async(_,_)=>{var child=Application.OpenForms.OfType<PurchaseEditor>().FirstOrDefault();if(child is null||child.Lines.Count==0||!child.SaveButton.Enabled)return;timer.Stop();try{clean=api.Writes==0&&child.Lines[0].Quantity==7;await Capture(child,"UI09-product-handoff-unsaved");await child.SaveAsync();saved=child.Current?.Lines.Any(l=>l.VariantId==3&&l.Quantity==7)==true;}catch(Exception e){failure=e;}finally{child.Close();}};
            timer.Start();product.PurchaseButton.PerformClick();Assert(failure is null&&clean&&saved&&api.Stocks[0].OnHand==10,"Handoff persisted early/lost hints/changed stock");product.Controls.OfType<TableLayoutPanel>().Single().Controls.OfType<TabControl>().Single().SelectedIndex=1;product.VariantGrid.FirstDisplayedScrollingColumnIndex=5;await Capture(product,"UI08-product-purchase-handoff");product.Close();
        });
        await Test("W4 finish F1/F2 visible confirmation recovery and held journal/picker paging do not skip pages",async()=>
        {
            using var api=new SupplyFake{PagingTotal=45};
            using(var editor=new PurchaseEditor(api,1)){editor.Show();await Task.Delay(60);editor.LinesGrid.Rows[0].Cells["Quantity"].Value=11;editor.LinesGrid.EndEdit();await Task.Delay(20);Assert(!editor.ConfirmButton.Enabled&&editor.ConfirmHintLabel.Text.Contains("chưa lưu"),"Disabled dirty confirmation lacks recovery");await Capture(editor,"UI09-unsaved-confirm-hint");editor.Close();}
            using(var journal=new StockJournalForm(api,api.Stocks[0]))
            {
                journal.Show();await Task.Delay(60);Assert(journal.NextButton.Enabled,"Journal paging fixture absent");api.ReadHold=new(TaskCreationOptions.RunContinuationsAsynchronously);journal.NextButton.PerformClick();await Task.Delay(20);var reads=api.Paths.Count;journal.NextButton.PerformClick();journal.PreviousButton.PerformClick();Assert(!journal.NextButton.Enabled&&!journal.PreviousButton.Enabled&&api.Paths.Count==reads,"Journal pager changed during load");await Capture(journal,"UI10-journal-loading");api.ReadHold.SetResult();await Task.Delay(50);api.ReadHold=null;Assert(journal.MessageLabel.Text.StartsWith("Trang 2")&&api.Paths.Last().Contains("page=2"),"Journal skipped or mislabeled pending page");journal.Close();
            }
            using(var picker=new SupplyVariantPicker(api))
            {
                picker.Show();await Task.Delay(60);Assert(picker.NextButton.Enabled,"Picker paging fixture absent");api.ReadHold=new(TaskCreationOptions.RunContinuationsAsynchronously);picker.NextButton.PerformClick();await Task.Delay(20);var reads=api.Paths.Count;picker.NextButton.PerformClick();picker.PreviousButton.PerformClick();Assert(!picker.NextButton.Enabled&&!picker.PreviousButton.Enabled&&!picker.ChooseButton.Enabled&&api.Paths.Count==reads,"Picker pager/selection active during load");await Capture(picker,"UI09-picker-loading");api.ReadHold.SetResult();await Task.Delay(50);api.ReadHold=null;Assert(picker.MessageLabel.Text.StartsWith("Trang 2")&&api.Paths.Last().Contains("page=2"),"Picker skipped or mislabeled pending page");picker.Close();
            }
        });
        File.WriteAllText(Path.Combine(output,"results.json"),System.Text.Json.JsonSerializer.Serialize(new{fakeApi=true,qaOnly=true,checks=passed-start,captures=captures.Skip(captureOffset).ToArray()},new System.Text.Json.JsonSerializerOptions{WriteIndented=true}));output=previous;Console.WriteLine($"{passed-start} admin W4 form checks passed.");
    }
}

internal sealed class SupplyFake:IAdminApi
{
    public const string StockVersion="AgAAAAAAAAA=";
    public AdminIdentity? Identity=>new(90,"qa-admin","ADMIN",DateTimeOffset.UtcNow.AddHours(8));public bool NeedsReconciliation { get; private set; }public event Action? SessionEnded{add{}remove{}}
    public int Writes;public int PagingTotal;public TaskCompletionSource? ReadHold;public TaskCompletionSource? Hold;public bool LoseCreate,LoseCommand;public Exception? WriteError,StockError;public string? LastPath;public PurchaseTransition? LastTransition;public StockAdjustment? LastAdjustment;public PurchaseRequest? LastCreation;public List<string> Paths { get; }=[];
    public Dictionary<long,PurchaseRecord> Purchases { get; }=[];private readonly Dictionary<Guid,long> keys=[];private long next=1;
    public List<StockRecord> Stocks { get; }=[new(3,2,"Điện thoại QA","W4-A","Hãng QA","Đen",128,10,2,8,StockVersion),new(4,2,"Điện thoại QA","W4-B","Hãng QA","Trắng",256,5,0,5,"AwAAAAAAAAA=")];
    public List<MovementRecord> Logs { get; }=[new(1,3,"W4-A","NHAP",10,0,DateTimeOffset.UtcNow,90,"qa-admin",null,1,null,Guid.NewGuid())];
    private ProductRecord Product=new(2,"W4-PHONE","Điện thoại QA",1,1,"QA","QA","",true,[new(3,"W4-A","Đen",128,10000000,20,12000000,true,10,2,"AQAAAAAAAAA="),new(4,"W4-B","Trắng",256,9000000,25,11250000,true,5,0,"BAAAAAAAAAA=")],[]);
    public SupplyFake(){Purchases[1]=new(1,"PN-QA-001",1,"NCC QA","NHAP",125000000,90,null,DateTimeOffset.UtcNow,null,"AQAAAAAAAAA=",[new(3,"W4-A","Điện thoại QA",10,8000000,80000000),new(4,"W4-B","Điện thoại QA",5,9000000,45000000)]);}
    public async Task<T> GetAsync<T>(string path,CancellationToken ct=default)
    {
        Paths.Add("GET "+path);if(ReadHold is not null)await ReadHold.Task.WaitAsync(ct);object result;
        if(path.StartsWith("/api/admin/lookups/"))result=new PageResult<LookupRow>([new(1,path.Contains("brands")?"Hãng QA":"Điện thoại QA",true)],1,1,100);
        else if(path.StartsWith("/api/admin/suppliers"))result=new PageResult<SupplierRecord>([new(1,"SUP-QA","NCC QA","0938123593","QA",true)],1,1,100);
        else if(path.StartsWith("/api/admin/products?"))result=new PageResult<ProductRecord>([Product],PagingTotal>0?PagingTotal:1,int.Parse(Parameter(path,"page")??"1"),20);
        else if(path.StartsWith("/api/admin/products/"))result=Product;
        else if(path.StartsWith("/api/admin/purchases?"))result=new PageResult<PurchaseRecord>(Purchases.Values.ToList(),Purchases.Count,1,20);
        else if(path.StartsWith("/api/admin/purchases/"))result=Purchases[long.Parse(path.Split('/')[4])];
        else if(path.StartsWith("/api/admin/stock/movements")){var key=Parameter(path,"operationKey");var variant=Parameter(path,"variantId");var rows=Logs.Where(l=>(key is null||l.OperationKey.ToString()==key)&&(variant is null||l.VariantId.ToString()==variant)).ToList();result=new PageResult<MovementRecord>(rows,PagingTotal>0?PagingTotal:rows.Count,int.Parse(Parameter(path,"page")??"1"),20);}
        else if(path.StartsWith("/api/admin/stock?")){if(StockError is not null)throw StockError;var query=Parameter(path,"query")??"";var rows=Stocks.Where(s=>query.Length==0||s.Sku.Contains(query)).ToList();result=new PageResult<StockRecord>(rows,rows.Count,1,path.Contains("pageSize=100")?100:20);}
        else throw new InvalidOperationException("Unexpected safe supply fixture path");return (T)result;
    }
    private static string? Parameter(string path,string name)=>path.Split('?').Skip(1).SelectMany(q=>q.Split('&')).Where(p=>p.StartsWith(name+"=",StringComparison.Ordinal)).Select(p=>Uri.UnescapeDataString(p[(name.Length+1)..])).FirstOrDefault();
    public Task<long> CreateAsync<T>(string path,T input,CancellationToken ct=default)
    {
        Writes++;LastCreation=(PurchaseRequest)(object)input!;if(WriteError is not null)throw WriteError;var id=Create(LastCreation);if(LoseCreate){LoseCreate=false;NeedsReconciliation=true;throw new AdminApiException("UNCERTAIN");}return Task.FromResult(id);
    }
    private long Create(PurchaseRequest request)
    {if(keys.TryGetValue(request.RequestKey,out var id))return id;id=++next;keys.Add(request.RequestKey,id);var lines=request.Lines.Select(l=>new PurchaseLineRecord(l.VariantId,Stocks.Single(s=>s.VariantId==l.VariantId).Sku,"Điện thoại QA",l.Quantity,l.Cost,l.Quantity*l.Cost)).ToList();Purchases[id]=new(id,"PN-QA-"+id,request.SupplierId,"NCC QA","NHAP",lines.Sum(l=>l.Total),90,null,DateTimeOffset.UtcNow,null,"AQAAAAAAAAA=",lines);return id;}
    public Task<long> RetryPurchaseCreateAsync(PurchaseRequest input,CancellationToken ct=default){Writes++;if(!NeedsReconciliation||LastCreation?.RequestKey!=input.RequestKey)throw new AdminApiException("UNCERTAIN");NeedsReconciliation=false;return Task.FromResult(Create(input));}
    public Task PutAsync<T>(string path,T input,CancellationToken ct=default)
    {Writes++;if(WriteError is not null)throw WriteError;var id=long.Parse(path.Split('/')[4]);var p=(PurchaseRequest)(object)input!;var old=Purchases[id];Purchases[id]=old with{Version="BQAAAAAAAAA=",Total=p.Lines.Sum(l=>l.Quantity*l.Cost),Lines=p.Lines.Select(l=>new PurchaseLineRecord(l.VariantId,Stocks.Single(s=>s.VariantId==l.VariantId).Sku,"Điện thoại QA",l.Quantity,l.Cost,l.Quantity*l.Cost)).ToList()};return Task.CompletedTask;}
    public async Task CommandAsync<T>(string path,T input,CommandIntent intent,CancellationToken ct=default)
    {
        Writes++;LastPath=path;if(input is PurchaseTransition p)LastTransition=p;if(input is StockAdjustment a)LastAdjustment=a;if(Hold is not null)await Hold.Task;if(WriteError is not null)throw WriteError;
        if(input is PurchaseTransition command){var id=long.Parse(path.Split('/')[4]);var row=Purchases[id];var confirmed=path.EndsWith("/confirm");Purchases[id]=row with{Status=confirmed?"DA_XAC_NHAN":"DA_HUY",Version="BgAAAAAAAAA="};if(confirmed){Product=Product with{Variants=Product.Variants.Select(v=>v.Id==3?v with{Cost=8000000,Price=9600000}:v).ToList()};}}
        if(input is StockAdjustment count){var old=Stocks[0];Stocks[0]=old with{OnHand=count.Counted,Available=count.Counted-old.Reserved,Version="BwAAAAAAAAA="};Logs.Add(new(Logs.Count+1,3,"W4-A","DIEU_CHINH",count.Counted-old.OnHand,0,DateTimeOffset.UtcNow,90,"qa-admin",null,null,count.Reason,count.OperationKey));}
        if(LoseCommand){LoseCommand=false;NeedsReconciliation=true;throw new AdminApiException("UNCERTAIN");}
    }
    public async Task ReconcileAsync(Func<CancellationToken,Task> read,CancellationToken ct=default){await read(ct);NeedsReconciliation=false;}
    public Task<ReadyInfo> ReadinessAsync(CancellationToken ct=default)=>Task.FromResult(new ReadyInfo("ready","QA · API giả","supply-fixture-no-server"));public Task<AdminIdentity> LoginAsync(string u,string p,CancellationToken ct=default)=>Task.FromResult(Identity!);public Task<AdminIdentity?> RecoverSessionAsync(CancellationToken ct=default)=>Task.FromResult(Identity);public Task LogoutAsync(CancellationToken ct=default)=>Task.CompletedTask;public Task ChangePasswordAsync(string a,string b,CancellationToken ct=default)=>Task.CompletedTask;public void Dispose(){}
}
