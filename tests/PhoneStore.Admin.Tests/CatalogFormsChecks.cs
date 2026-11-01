using PhoneStore.Admin.Client;

namespace PhoneStore.Admin.Tests;

internal static partial class Program
{
    private static async Task RealCatalog()
    {
        var address=Environment.GetEnvironmentVariable("PHONESTORE_ADMIN_QA_URL")??throw new InvalidOperationException("QA URL missing");
        if(!ServerAddress.Validate(address).IsLoopback)throw new InvalidOperationException("QA loopback only");
        using var api=new ApiClient(new(address,"Testing"));await api.LoginAsync(Environment.GetEnvironmentVariable("PHONESTORE_ADMIN_QA_USERNAME")!,Environment.GetEnvironmentVariable("PHONESTORE_ADMIN_QA_PASSWORD")!);
        var service=new CatalogWorkspace(api);await service.SaveLookup("brands",0,new("Hãng QA thực",true));await service.SaveLookup("categories",0,new("Điện thoại QA thực",true));
        using(var editor=new ProductEditor(api)){editor.Show();for(var i=0;i<100&&editor.BrandBox.Items.Count==0;i++)await Task.Delay(50);editor.CodeBox.Text="QA-DESKTOP-W2";editor.NameBox.Text="Điện thoại QA · API thực";editor.Variants[0].Sku="QA-DESKTOP-B";editor.Variants[0].Color="Đen";editor.Variants[0].Cost=10000000;editor.ImageBox.Text="/images/qa.png";await editor.SaveAsync();Assert(editor.Saved is not null&&editor.Saved.Variants.Single().Price==12000000,"Real editor create failed: "+editor.MessageLabel.Text);await Capture(editor,"UI08-real-qa-editor");editor.Close();}
        using(var list=new ProductListForm(api)){list.Show();for(var i=0;i<100&&list.Grid.Rows.Count==0;i++)await Task.Delay(50);Assert(list.Grid.Rows.Count==1,"Real QA product absent");await Capture(list,"UI08-real-qa-list");list.Close();}
        await api.LogoutAsync();File.WriteAllText(Path.Combine(output,"results.json"),System.Text.Json.JsonSerializer.Serialize(new{fakeApi=false,qaOnly=true,captures},new System.Text.Json.JsonSerializerOptions{WriteIndented=true}));Console.WriteLine("PASS WIN.A05.01/A04.01 real controls -> HTTP API -> isolated SQL QA");passed++;
    }
    private static async Task CatalogFormsChecks()
    {
        var previousOutput=output;output=Path.Combine(FindRoot(),".artifacts","admin-w2","forms");Directory.CreateDirectory(output);var first=passed;var firstCapture=captures.Count;
        await Test("WIN.A04.01/03 actual list filters/paging, inactive label resolution and no-match/error",async()=>
        {
            using var api=new CatalogFake();using var form=new ProductListForm(api);form.Show();await Task.Delay(90);
            Assert(form.Grid.Rows.Count==2&&form.Grid.Rows[1].Cells["State"].Value?.ToString()=="Đã ngừng","Inactive state missing");
            Assert(form.Grid.Rows[1].Cells["Brand"].Value?.ToString()=="Hãng đã ngừng QA","Inactive ADMIN name not resolved");await Capture(form,"UI08-product-list");
            form.QueryBox.Text="no-match";form.SearchButton.PerformClick();await Task.Delay(50);Assert(form.Grid.Rows.Count==0&&form.MessageLabel.Text.Contains("Không có"),"No-match state wrong");await Capture(form,"UI08-product-empty");
            api.FailRead=true;await form.LoadAsync();Assert(form.MessageLabel.Text.Contains("chưa sẵn sàng"),"Read failure hidden as empty");form.Close();
        });
        await Test("WIN.A05.01/02/03/05 new editor validation retains form, server errors retained, POST201 then GET",async()=>
        {
            using var api=new CatalogFake();using var form=new ProductEditor(api);form.Show();await Task.Delay(70);form.CodeBox.Text="QA-NEW";form.NameBox.Text="Điện thoại mới QA";form.Variants[0].Sku="QA-NEW-B";form.Variants[0].Color="Đen";form.Variants[0].Cost=10000000;
            foreach(var url in new[]{"javascript:alert(1)","data:x","//host/a","http://host/a","/images/../a"}){form.ImageBox.Text=url;await form.SaveAsync();Assert(api.Writes==0&&form.ImageBox.Text==url,"Invalid URL not retained/blocked");}
            form.ImageBox.Text="/images/qa.png";form.Variants[0].SuggestedQuantity=0;await form.SaveAsync();Assert(api.Writes==0,"Invalid suggestion sent");form.Variants[0].SuggestedQuantity=1;
            api.WriteError=new AdminApiException("INVALID_INPUT",400);await form.SaveAsync();Assert(form.NameBox.Text=="Điện thoại mới QA"&&form.Variants[0].Sku=="QA-NEW-B"&&form.SaveButton.Enabled,"Server error lost draft");await Capture(form,"UI08-product-error-retained");
            api.WriteError=null;await form.SaveAsync();Assert(api.CreateCalls==2&&form.Saved?.Id==1&&form.Variants[0].Price==12000000,"Create did not read authoritative product");Assert(!form.PurchaseButton.Enabled,"W4 purchase opened early");form.Close();
        });
        await Test("WIN.A06.01/02/03/04 editor GET complete PUT, old cost read-only, server price and stale retention",async()=>
        {
            using var api=new CatalogFake();using var form=new ProductEditor(api,1,(_,_)=>true);form.Show();await Task.Delay(70);await Capture(form,"UI08-product-editor-general");
            var tabs=form.Controls.OfType<TableLayoutPanel>().Single().Controls.OfType<TabControl>().Single();tabs.SelectedIndex=1;await Capture(form,"UI08-product-editor-variants");
            Assert(!form.VariantGrid.AllowUserToDeleteRows&&form.VariantGrid.Columns["Price"]!.ReadOnly&&form.VariantGrid.Rows[0].Cells["Cost"].ReadOnly,"Old cost/price or delete editable");
            form.Variants[0].Cost=99;form.Variants[0].ProfitPercent=25;form.AddVariantButton.PerformClick();form.Variants.Last().Sku="QA-ADDED";form.Variants.Last().Color="Xanh";
            api.WriteError=new AdminApiException("CONCURRENT_UPDATE",409);await form.SaveAsync();Assert(form.MessageLabel.Text.Contains("vui lòng tải lại")&&form.Variants.Count==3&&form.Variants[0].ProfitPercent==25,"Stale conflict erased draft");
            Assert(api.LastRequest?.Variants.Count==3&&api.LastRequest.Variants[0].Version=="AAAAAAAAAAE="&&api.LastRequest.Variants[0].Cost==10000000,"Incomplete/mutated old variant request");
            api.WriteError=null;await form.SaveAsync();Assert(form.Variants[0].Price==12500000,"Displayed price not server reply");Assert(form.Handoff().ProductId==1&&!form.PurchaseButton.Enabled,"Typed W4 handoff missing");form.Close();
        });
        await Test("WIN.A07.01/02 and A08.01/02/03 confirmation gates full-state commands and retains rejected lookup draft",async()=>
        {
            using var api=new CatalogFake();var prompts=new List<string>();var allow=false;
            using(var list=new ProductListForm(api,(t,m)=>{prompts.Add(m);return allow;})){list.Show();await Task.Delay(70);await list.ChangeStateAsync();Assert(api.Writes==0,"Cancelled stop sent PUT");allow=true;await list.ChangeStateAsync();Assert(api.LastRequest is{Active:false}&&api.LastRequest.Variants.Count==2,"Product stop incomplete");list.Close();}
            using(var lookup=new LookupForm(api,"brands",(t,m)=>{prompts.Add(m);return allow;})){lookup.Show();await Task.Delay(60);lookup.NameBox.Text="Tên sửa QA";lookup.ActiveBox.Checked=false;allow=false;var count=api.Writes;await lookup.SaveAsync();Assert(api.Writes==count,"Cancelled lookup stop wrote");allow=true;api.WriteError=new AdminApiException("INVALID_INPUT",400);await lookup.SaveAsync();Assert(lookup.NameBox.Text=="Tên sửa QA"&&!lookup.ActiveBox.Checked,"Lookup error lost fields");api.WriteError=null;await lookup.SaveAsync();Assert(api.LastLookup is{Name:"Tên sửa QA",Active:false},"Lookup full-state DTO missing");await Capture(lookup,"lookup-admin-inactive");var creates=api.CreateCalls;lookup.NewButton.PerformClick();lookup.NameBox.Text="Hãng mới QA";await lookup.SaveAsync();Assert(api.CreateCalls==creates+1,"New lookup reused selected update id");lookup.Close();}
            using(var lookup=new LookupForm(api,"brands",(_,_)=>true))
            {
                lookup.Show();await Task.Delay(50);lookup.NewButton.PerformClick();lookup.NameBox.Text="Z Hãng mới QA";await lookup.SaveAsync();
                Assert(lookup.Grid.CurrentRow?.Tag is LookupRow{Name:"Z Hãng mới QA"}&&lookup.NameBox.Text=="Z Hãng mới QA","Created lookup selection points at another record");lookup.Close();
            }
            Assert(prompts.Any(p=>p.Contains("không còn hiển thị ở website")),"DEC03 warning missing");
            using var dialog=new ConfirmationDialog("Ngừng kinh doanh","Sản phẩm liên quan sẽ không còn hiển thị ở website. Dữ liệu và lịch sử được giữ nguyên.");dialog.Show();await Capture(dialog,"catalog-stop-confirmation");dialog.DismissButton.PerformClick();
        });
        await Test("WIN.A04.02/03 actual details/read error and Main catalog navigation opens real child form",async()=>
        {
            using var api=new CatalogFake();using(var detail=new ProductDetailsForm(api,1)){detail.Show();await Task.Delay(70);Assert(detail.DetailsBox.Text.Contains("/images/qa.png")&&detail.Grid.Rows.Count==2,"Detail missing");await Capture(detail,"UI08-product-details");api.FailRead=true;await detail.LoadAsync();Assert(detail.MessageLabel.Text.Contains("chưa sẵn sàng"),"Detail failure hidden");detail.Close();api.FailRead=false;}
            using var main=new MainForm(api,CatalogFake.Ready);main.Show();using var timer=new System.Windows.Forms.Timer{Interval=100};var opened=false;timer.Tick+=(_,_)=>{var child=Application.OpenForms.OfType<ProductListForm>().FirstOrDefault();if(child is not null){opened=true;child.Close();timer.Stop();}};timer.Start();main.Navigation.Single(b=>b.Text=="Điện thoại").PerformClick();Assert(opened,"Catalog navigation not wired");main.EndSession();
        });
        File.WriteAllText(Path.Combine(output,"results.json"),System.Text.Json.JsonSerializer.Serialize(new{fakeApi=true,qaOnly=true,checks=passed-first,captures=captures.Skip(firstCapture).ToArray()},new System.Text.Json.JsonSerializerOptions{WriteIndented=true}));output=previousOutput;
        Console.WriteLine($"{passed-first} admin W2 form checks passed.");
    }
}

internal sealed class CatalogFake:IAdminApi
{
    public static ReadyInfo Ready=>new("ready","QA · API giả","fixture-catalog-no-server");
    public AdminIdentity? Identity=>new(1,"qa-admin","ADMIN",DateTimeOffset.UtcNow.AddHours(8));
    public bool NeedsReconciliation=>false;
    public event Action? SessionEnded { add{} remove{} }
    public int Writes,CreateCalls;public bool FailRead;public Exception? WriteError;
    public ProductRequest? LastRequest;public LookupRequest? LastLookup;
    public ProductRecord Product=new(1,"QA-PHONE","Điện thoại QA",1,1,"Thông số QA","Mô tả QA","/images/qa.png",true,
        [new(1,"QA-B","Đen",128,10000000,20,12000000,true,5,1,"AAAAAAAAAAE="),new(2,"QA-W","Trắng",256,12000000,0,12000000,true,0,0,"AAAAAAAAAAI=")],[new("https://example.test/qa.png",0)]);
    public List<LookupRow> Brands=[new(1,"Hãng QA",true),new(2,"Hãng đã ngừng QA",false)];
    public Task<T> GetAsync<T>(string path,CancellationToken ct=default)
    {
        if(FailRead)throw new AdminApiException("DATABASE_UNAVAILABLE",503);
        object result=path.StartsWith("/api/admin/lookups/brands")?new PageResult<LookupRow>(Brands,Brands.Count,1,100):path.StartsWith("/api/admin/lookups/categories")?new PageResult<LookupRow>([new(1,"Điện thoại QA",true)],1,1,100):path.StartsWith("/api/admin/products?")?new PageResult<ProductRecord>(path.Contains("no-match")?[]:[Product,Product with{Id=2,Code="QA-STOP",BrandId=2,Active=false}],path.Contains("no-match")?0:2,1,20):Product;
        return Task.FromResult((T)result);
    }
    public Task<long> CreateAsync<T>(string path,T input,CancellationToken ct=default)
    {
        Writes++;CreateCalls++;
        if(input is LookupRequest l){LastLookup=l;if(WriteError is not null)throw WriteError;var id=Brands.Max(b=>b.Id)+1;Brands.Add(new(id,l.Name,l.Active));return Task.FromResult((long)id);}
        Apply(input);return Task.FromResult(1L);
    }
    public Task PutAsync<T>(string path,T input,CancellationToken ct=default){Writes++;Apply(input);return Task.CompletedTask;}
    private void Apply<T>(T input)
    {
        if(input is ProductRequest p)LastRequest=p;if(input is LookupRequest l)LastLookup=l;if(WriteError is not null)throw WriteError;
        if(input is ProductRequest request)Product=Product with{Active=request.Active,Variants=Product.Variants.Select((v,i)=>i==0?v with{Price=request.Variants[0].ProfitPercent==25?12500000:12000000}:v).ToList()};
        if(input is LookupRequest lookup)Brands[0]=Brands[0] with{Name=lookup.Name,Active=lookup.Active};
    }
    public Task ReconcileAsync(Func<CancellationToken,Task> read,CancellationToken ct=default)=>read(ct);
    public Task<ReadyInfo> ReadinessAsync(CancellationToken ct=default)=>Task.FromResult(Ready);
    public Task<AdminIdentity> LoginAsync(string u,string p,CancellationToken ct=default)=>Task.FromResult(Identity!);
    public Task<AdminIdentity?> RecoverSessionAsync(CancellationToken ct=default)=>Task.FromResult(Identity);
    public Task LogoutAsync(CancellationToken ct=default)=>Task.CompletedTask;
    public Task ChangePasswordAsync(string a,string b,CancellationToken ct=default)=>Task.CompletedTask;
    public Task CommandAsync<T>(string p,T input,CommandIntent intent,CancellationToken ct=default)=>throw new NotSupportedException();
    public void Dispose(){}
}
