using PhoneStore.Admin.Client;

namespace PhoneStore.Admin.Tests;

internal static partial class Program
{
    private static async Task RealContacts()
    {
        var address=Environment.GetEnvironmentVariable("PHONESTORE_ADMIN_QA_URL")??throw new InvalidOperationException("QA URL missing");if(!ServerAddress.Validate(address).IsLoopback)throw new InvalidOperationException("QA loopback only");
        using var api=new ApiClient(new(address,"Testing"));await api.LoginAsync(Environment.GetEnvironmentVariable("PHONESTORE_ADMIN_QA_USERNAME")!,Environment.GetEnvironmentVariable("PHONESTORE_ADMIN_QA_PASSWORD")!);
        var customer=(await new ContactsWorkspace(api).Customers(null,null)).Items.Single();
        using(var profile=new CustomerProfileEditor(api,customer.Id)){profile.Show();for(var i=0;i<100&&!profile.SaveButton.Enabled;i++)await Task.Delay(50);profile.FullNameBox.Text="Khách QA W3 · API thực";profile.EmailBox.Text="desktop-w3@example.test";await profile.SaveAsync();Assert(profile.Saved?.FullName=="Khách QA W3 · API thực","Real profile editor failed: "+profile.MessageLabel.Text);await Capture(profile,"UI11-real-qa-profile");profile.Close();}
        using(var list=new CustomerListForm(api)){list.Show();for(var i=0;i<100&&list.Grid.Rows.Count==0;i++)await Task.Delay(50);Assert(list.Grid.Rows.Count==1,"Real customer list absent");await Capture(list,"UI11-real-qa-customers");list.Close();}
        using(var supplier=new SupplierEditor(api)){supplier.Show();await Task.Delay(50);supplier.CodeBox.Text="QA-DESKTOP-W3";supplier.NameBox.Text="Nhà cung cấp QA · API thực";supplier.PhoneBox.Text="0938123598";supplier.AddressBox.Text="Địa chỉ NCC QA thực";await supplier.SaveAsync();Assert(supplier.Saved is not null,"Real supplier editor failed: "+supplier.MessageLabel.Text);await Capture(supplier,"supplier-real-qa-editor");supplier.Close();}
        using(var list=new SupplierListForm(api)){list.Show();for(var i=0;i<100&&list.Grid.Rows.Count==0;i++)await Task.Delay(50);Assert(list.Grid.Rows.Count==1,"Real supplier list absent");await Capture(list,"supplier-real-qa-list");list.Close();}
        using(var picker=new SupplierPickerForm(api)){picker.Show();for(var i=0;i<100&&!picker.ChooseButton.Enabled;i++)await Task.Delay(50);Assert(picker.SuppliersBox.Items.Count==1,"Real active supplier picker absent");await Capture(picker,"supplier-real-qa-picker");picker.Close();}
        await api.LogoutAsync();File.WriteAllText(Path.Combine(output,"results.json"),System.Text.Json.JsonSerializer.Serialize(new{fakeApi=false,qaOnly=true,captures},new System.Text.Json.JsonSerializerOptions{WriteIndented=true}));Console.WriteLine("PASS WIN.A10/A13 real controls -> HTTP API -> isolated SQL QA");passed++;
    }
    private static async Task ContactFormsChecks()
    {
        var previous=output;output=Path.Combine(FindRoot(),".artifacts","admin-w3","forms");Directory.CreateDirectory(output);var first=passed;var captureOffset=captures.Count;
        await Test("WIN.A09.01/02 actual customer list filters/empty/error and safe detail",async()=>
        {
            using var api=new ContactsFake();using(var list=new CustomerListForm(api)){list.Show();await Task.Delay(80);Assert(list.Grid.Rows.Count==2&&list.Grid.Columns.Cast<DataGridViewColumn>().All(c=>!c.Name.Contains("Password")&&!c.Name.Contains("Hash")),"Customer list contains secrets");await Capture(list,"UI11-customer-list");list.QueryBox.Text="no-match";list.SearchButton.PerformClick();await Task.Delay(40);Assert(list.Grid.Rows.Count==0&&list.MessageLabel.Text.Contains("Không có"),"Customer empty wrong");await Capture(list,"UI11-customer-empty");api.ReadError=new AdminApiException("DATABASE_UNAVAILABLE",503);await list.LoadAsync();Assert(list.MessageLabel.Text.Contains("chưa sẵn sàng"),"Read error hidden");list.Close();api.ReadError=null;}
            using var detail=new CustomerDetailsForm(api,1);detail.Show();await Task.Delay(50);Assert(detail.Current?.Id==1&&detail.OrdersButton.Enabled&&!detail.DetailsBox.Text.Contains("hash",StringComparison.OrdinalIgnoreCase),"Safe customer detail missing");await Capture(detail,"UI11-customer-details");detail.Close();
        });
        await Test("WIN.A10.01/02 actual four-field profile form retains duplicate errors, then PUT/readback",async()=>
        {
            using var api=new ContactsFake();using var editor=new CustomerProfileEditor(api,1);editor.Show();await Task.Delay(60);
            IEnumerable<Control> Descendants(Control root)=>root.Controls.Cast<Control>().SelectMany(c=>new[]{c}.Concat(Descendants(c)));
            Assert(Descendants(editor).OfType<TextBox>().Count()==4&&!Descendants(editor).OfType<CheckBox>().Any(),"Profile editor exposes role/state/password fields");
            editor.FullNameBox.Text="Khách QA đã nhập";editor.PhoneBox.Text="0938123592";editor.EmailBox.Text="peer@example.test";editor.AddressBox.Text="Địa chỉ vẫn giữ QA";
            api.WriteError=new AdminApiException("INVALID_INPUT",400);await editor.SaveAsync();Assert(editor.FullNameBox.Text=="Khách QA đã nhập"&&editor.EmailBox.Text=="peer@example.test"&&editor.PhoneBox.Text=="0938123592"&&editor.SaveButton.Enabled,"Duplicate error lost form");await Capture(editor,"UI11-customer-profile-error");
            api.WriteError=null;await editor.SaveAsync();Assert(editor.Saved?.FullName=="Khách QA đã nhập"&&api.LastProfile?.Email=="peer@example.test"&&api.CustomerReads>=2,"No profile readback");await Capture(editor,"UI11-customer-profile-saved");editor.Close();
        });
        await Test("WIN.A11.01/02/03 confirmations/full state, stale reload message, ADMIN has no lock action",async()=>
        {
            using var api=new ContactsFake();var allow=false;var prompts=new List<string>();using(var detail=new CustomerDetailsForm(api,1,(t,m)=>{prompts.Add(m);return allow;})){detail.Show();await Task.Delay(50);await detail.ChangeStateAsync();Assert(api.Writes==0,"Cancelled lock wrote");allow=true;api.WriteError=new AdminApiException("CONCURRENT_UPDATE",409);await detail.ChangeStateAsync();Assert(detail.MessageLabel.Text.Contains("vui lòng tải lại")&&api.LastState is{Active:false,ExpectedActive:true},"Stale expectedActive message/body wrong");api.WriteError=null;await detail.ChangeStateAsync();Assert(detail.Current is{Active:false}&&detail.MessageLabel.Text.Contains("Mọi phiên"),"Lock confirmation text wrong");await Capture(detail,"UI11-customer-locked");await detail.ChangeStateAsync();Assert(api.LastState is{Active:true,ExpectedActive:false}&&detail.MessageLabel.Text.Contains("đăng nhập lại"),"Unlock promises wrong session behavior");detail.Close();}
            using(var admin=new CustomerDetailsForm(api,99)){admin.Show();await Task.Delay(50);Assert(admin.Current is null&&!admin.StateButton.Visible&&!admin.EditButton.Enabled,"ADMIN has a lock/profile action");admin.Close();}
            Assert(prompts.Any(p=>p.Contains("không khôi phục")),"Unlock warning absent");
        });
        await Test("WIN.A12.01 actual customer orders/empty and handoff opens read-only HTTP detail",async()=>
        {
            using var api=new ContactsFake();using(var orders=new CustomerOrdersForm(api,1)){orders.Show();await Task.Delay(50);Assert(orders.Grid.Rows.Count==1&&orders.DetailsButton.Enabled,"Customer orders missing");await Capture(orders,"customer-orders");using var timer=new System.Windows.Forms.Timer{Interval=100};var opened=false;timer.Tick+=(_,_)=>{var form=Application.OpenForms.OfType<OrderPeekForm>().FirstOrDefault();if(form is not null){opened=form.DetailsBox.Text.Contains("W3-ORDER-QA")&&form.DetailsBox.ReadOnly;form.Close();timer.Stop();}};timer.Start();orders.DetailsButton.PerformClick();Assert(opened,"Default W3 order detail handoff did not open");orders.Close();}
            using(var empty=new CustomerOrdersForm(api,2)){empty.Show();await Task.Delay(40);Assert(empty.MessageLabel.Text.Contains("chưa có đơn")&&!empty.DetailsButton.Enabled,"Empty customer orders wrong");empty.Close();}
            using var peek=new OrderPeekForm(api,33);peek.Show();await Task.Delay(40);await Capture(peek,"customer-order-readonly-detail");peek.Close();
        });
        await Test("WIN.A13.01/02/03 supplier new/edit/duplicate retention, no deletion and active-only picker",async()=>
        {
            using var api=new ContactsFake();using(var list=new SupplierListForm(api)){list.Show();await Task.Delay(50);Assert(list.Grid.Rows.Count==2&&!list.Grid.AllowUserToDeleteRows,"Supplier list missing/no-delete wrong");await Capture(list,"supplier-list");list.Close();}
            using(var editor=new SupplierEditor(api,api.Supplier,(_,_)=>true)){editor.Show();await Task.Delay(50);Assert(editor.CodeBox.Text==api.Supplier.Code,"Supplier list-based initial load failed");editor.NameBox.Text="Tên NCC giữ lại QA";editor.CodeBox.Text="DUPLICATE";api.WriteError=new AdminApiException("INVALID_INPUT",400);await editor.SaveAsync();Assert(editor.CodeBox.Text=="DUPLICATE"&&editor.NameBox.Text=="Tên NCC giữ lại QA"&&editor.SaveButton.Enabled,"Duplicate supplier lost draft");await Capture(editor,"supplier-editor-error");api.WriteError=null;editor.CodeBox.Text="QA-SUP-UPDATED";await editor.SaveAsync();Assert(editor.Saved?.Code=="QA-SUP-UPDATED"&&api.SupplierReads>=2,"No supplier list readback");editor.Close();}
            using(var create=new SupplierEditor(api)){create.Show();await Task.Delay(30);create.CodeBox.Text="QA-SUP-NEW";create.NameBox.Text="Nhà cung cấp mới QA";create.PhoneBox.Text="0938123598";create.AddressBox.Text="Địa chỉ NCC mới QA";await create.SaveAsync();Assert(create.Saved?.Id==3&&api.Creates==1,"Supplier create/readback failed");create.Close();}
            using var picker=new SupplierPickerForm(api);picker.Show();await Task.Delay(50);Assert(picker.SuppliersBox.Items.Cast<SupplierRecord>().All(s=>s.Active)&&picker.SuppliersBox.Items.Count==2,"Picker exposes stopped supplier");await Capture(picker,"supplier-active-picker");picker.ChooseButton.PerformClick();Assert(picker.Selected is{Active:true},"Picker did not return selection");
            Assert(api.Paths.All(p=>!p.StartsWith("GET /api/admin/suppliers/")),"Invented supplier by-id route");
        });
        File.WriteAllText(Path.Combine(output,"results.json"),System.Text.Json.JsonSerializer.Serialize(new{fakeApi=true,qaOnly=true,checks=passed-first,captures=captures.Skip(captureOffset).ToArray()},new System.Text.Json.JsonSerializerOptions{WriteIndented=true}));output=previous;Console.WriteLine($"{passed-first} admin W3 form checks passed.");
    }
}

internal sealed class ContactsFake:IAdminApi
{
    public AdminIdentity? Identity=>new(90,"qa-admin","ADMIN",DateTimeOffset.UtcNow.AddHours(8));public bool NeedsReconciliation=>false;public event Action? SessionEnded{add{}remove{}}
    public int Writes,Creates,CustomerReads,SupplierReads;public Exception? WriteError,ReadError;public CustomerProfileRequest? LastProfile;public CustomerStateRequest? LastState;public List<string> Paths { get; }=[];
    public CustomerRecord Customer=new(1,"customer-qa","Khách hàng QA","0938123591","customer@example.test","Địa chỉ QA",true,DateTimeOffset.UtcNow);
    public SupplierRecord Supplier=new(1,"QA-SUP-ONE","Nhà cung cấp QA","0938123593","Địa chỉ NCC QA",true);
    private readonly List<SupplierRecord> suppliers=[];
    public Task<T> GetAsync<T>(string path,CancellationToken ct=default)
    {
        Paths.Add("GET "+path);if(ReadError is not null)throw ReadError;object row;
        if(path.StartsWith("/api/admin/customers/99"))throw new AdminApiException("NOT_FOUND",404);
        if(path.Contains("/orders?")){var empty=path.StartsWith("/api/admin/customers/2/");row=new PageResult<CustomerOrder>(empty?[]:[Order],empty?0:1,1,20);}
        else if(path=="/api/admin/orders/33")row=Order;
        else if(path.StartsWith("/api/admin/customers?"))row=new PageResult<CustomerRecord>(path.Contains("no-match")?[]:[Customer,Customer with{Id=2,Username="customer-locked-qa",FullName="Khách QA đã khóa",Phone="0938123592",Email="peer@example.test",Active=false}],path.Contains("no-match")?0:2,1,20);
        else if(path.StartsWith("/api/admin/customers/")){CustomerReads++;row=Customer;}
        else if(path.StartsWith("/api/admin/suppliers?")){SupplierReads++;var rows=new[]{Supplier,new SupplierRecord(2,"QA-SUP-STOP","NCC đã ngừng QA","0938123594","QA",false)}.Concat(suppliers).Where(s=>!path.Contains("active=true")||s.Active).ToList();row=new PageResult<SupplierRecord>(rows,rows.Count,1,path.Contains("pageSize=100")?100:20);}
        else throw new InvalidOperationException("Unexpected safe fake path");return Task.FromResult((T)row);
    }
    private static CustomerOrder Order=>new(33,"W3-ORDER-QA",1,"Người nhận snapshot QA","0938123591","Địa chỉ đơn cũ QA","CHO_XAC_NHAN","CHUA_THU",12000000,DateTimeOffset.UtcNow,[new(1,"Điện thoại QA","QA-B","128GB",1,12000000,12000000)]);
    public Task PutAsync<T>(string path,T input,CancellationToken ct=default)
    {
        Writes++;Paths.Add("PUT "+path);if(input is CustomerProfileRequest p)LastProfile=p;if(WriteError is not null)throw WriteError;
        if(input is CustomerProfileRequest profile)Customer=Customer with{FullName=profile.FullName,Phone=profile.Phone,Email=profile.Email,Address=profile.Address};
        if(input is SupplierRequest s)Supplier=Supplier with{Code=s.Code,Name=s.Name,Phone=s.Phone,Address=s.Address,Active=s.Active};return Task.CompletedTask;
    }
    public Task<long> CreateAsync<T>(string path,T input,CancellationToken ct=default){Writes++;Creates++;Paths.Add("POST "+path);if(WriteError is not null)throw WriteError;var s=(SupplierRequest)(object)input!;suppliers.Add(new(3,s.Code,s.Name,s.Phone,s.Address,s.Active));return Task.FromResult(3L);}
    public Task CommandAsync<T>(string path,T input,CommandIntent intent,CancellationToken ct=default){Writes++;Paths.Add("POST "+path);LastState=(CustomerStateRequest)(object)input!;if(WriteError is not null)throw WriteError;Customer=Customer with{Active=LastState.Active};return Task.CompletedTask;}
    public Task ReconcileAsync(Func<CancellationToken,Task> read,CancellationToken ct=default)=>read(ct);
    public Task<ReadyInfo> ReadinessAsync(CancellationToken ct=default)=>Task.FromResult(new ReadyInfo("ready","QA · API giả","fixture-contacts-no-server"));
    public Task<AdminIdentity> LoginAsync(string u,string p,CancellationToken ct=default)=>Task.FromResult(Identity!);public Task<AdminIdentity?> RecoverSessionAsync(CancellationToken ct=default)=>Task.FromResult(Identity);public Task LogoutAsync(CancellationToken ct=default)=>Task.CompletedTask;public Task ChangePasswordAsync(string a,string b,CancellationToken ct=default)=>Task.CompletedTask;public void Dispose(){}
}
