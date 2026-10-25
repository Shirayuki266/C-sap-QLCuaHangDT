using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using PhoneStore.Core.Contracts;
using PhoneStore.Infrastructure.Data;
using PhoneStore.Infrastructure.Services;
internal static class DemoData
{
    sealed class Progress { public Dictionary<string,long> Purchases {get;set;}=[];public List<long> Orders {get;set;}=[];public bool Complete {get;set;} }
    static Guid Key(string text)=>new(SHA256.HashData(Encoding.UTF8.GetBytes("PhoneStoreDemoV1:"+text))[..16]);
    static DateTime At(int y,int m,int day)=>new DateTime(y,m,day,10,0,0,DateTimeKind.Utc).AddHours(-7);
    public static async Task Run(string root){
        var json=new JsonSerializerOptions{PropertyNameCaseInsensitive=true,WriteIndented=true};
        var state=JsonSerializer.Deserialize<QaEnvironment.State>(await File.ReadAllTextAsync(Path.Combine(root,".artifacts/qa-env/private.json")),json)!;
        QaTarget.Validate(state.ConnectionString,QaEnvironment.Config(root).Server,state.Database);
        var folder=Path.Combine(root,".artifacts/demo-data");Directory.CreateDirectory(folder);var file=Path.Combine(folder,state.Database+".json");
        var progress=File.Exists(file)?JsonSerializer.Deserialize<Progress>(await File.ReadAllTextAsync(file),json)!:new Progress();
        var options=new DbContextOptionsBuilder<StoreDbContext>().UseSqlServer(state.ConnectionString).Options;
        StoreDbContext Open()=>new(options);
        async Task<T> Use<T>(Func<StoreService,Task<T>> action){await using var db=Open();return await action(new(db));}
        async Task Save()=>await File.WriteAllTextAsync(file,JsonSerializer.Serialize(progress,json));
        await using var gate=new SqlConnection(state.ConnectionString);await gate.OpenAsync();await using var mutex=gate.CreateCommand();mutex.CommandText="DECLARE @r int;EXEC @r=sp_getapplock @Resource='PhoneStore:DemoDataV1',@LockMode='Exclusive',@LockOwner='Session',@LockTimeout=0;IF @r<0 THROW 51000,'Demo seed is already running',1;";await mutex.ExecuteNonQueryAsync();
        long admin;int category;await using(var db=Open()){admin=(await db.Accounts.SingleAsync(x=>x.Username==state.AdminUsername&&x.Role=="ADMIN")).Id;category=(await db.Categories.FirstAsync()).Id;}
        if(!progress.Complete){
            var brands=new List<int>();foreach(var name in new[]{"Apple","Samsung","Xiaomi","OPPO","vivo","realme"}){await using var db=Open();var existing=await db.Brands.FirstOrDefaultAsync(x=>x.Name==name);brands.Add(existing?.Id??await Use(s=>s.SaveLookup(admin,"brands",0,new(name,true))));}
            var suppliers=new List<long>();for(int i=0;i<4;i++){var code="DEMO-NCC-"+i;await using var db=Open();var old=await db.Suppliers.FirstOrDefaultAsync(x=>x.Code==code);suppliers.Add(old?.Id??await Use(s=>s.SaveSupplier(admin,0,new(code,"Nhà phân phối mẫu "+(i+1),null,"Địa chỉ kiểm thử, TP. Hồ Chí Minh",true))));}
            var firstNames=new[]{"An","Bình","Chi","Dũng","Giang","Hà","Khánh","Linh","Minh","Ngọc"};var customers=new List<long>();
            for(int i=0;i<40;i++){var username="demo.customer"+(i+1).ToString("D3");await using var db=Open();var old=await db.Accounts.SingleOrDefaultAsync(x=>x.Username==username);customers.Add(old?.Id??await Use(s=>s.Register(new(username,"Demo1!"+Guid.NewGuid().ToString("N"),"Khách mẫu "+firstNames[i%10]+" "+(i+1),"0977"+(i+1).ToString("D6"),username+"@example.test","Địa chỉ mẫu số "+(i+1)+", Quận 1, TP. Hồ Chí Minh"))));}
            var models=new[]{"iPhone 13","iPhone 14","iPhone 15","iPhone 16","Galaxy A15","Galaxy A25","Galaxy A55","Galaxy S24","Redmi Note 13","Redmi Note 14","Xiaomi 14","POCO X6","OPPO A60","OPPO A78","OPPO Reno11","OPPO Reno12","vivo Y28","vivo Y36","vivo V30","vivo V40","realme C55","realme C67","realme 12","realme GT6"};
            var variants=new List<VariantDto>();
            for(int i=0;i<24;i++){var code="DEMO-P"+(i+1).ToString("D3");long id;await using(var db=Open())id=await db.Products.Where(x=>x.Code==code).Select(x=>x.Id).FirstOrDefaultAsync();if(id==0){var price=2990000m+(23-i)*450000m;id=await Use(s=>s.SaveProduct(admin,0,new(code,models[i]+" · Bản mẫu",brands[i/4],category,"Màn hình AMOLED · RAM 8GB · Camera 50MP","Sản phẩm dữ liệu mẫu phục vụ đồ án; thông số và giá không phải báo giá thực tế.","/images/phone.svg",true,Enumerable.Range(0,3).Select(j=>new VariantInput(0,code+"-"+j,new[]{"Đen","Xanh","Trắng"}[j],new[]{128,256,512}[j],price+j*900000m,true)).ToList())));}variants.AddRange((await Use(s=>s.ProductDetail(id,true,admin))).Variants);}
            var sellable=variants.Where((v,i)=>i%12!=0).ToList();var today=DateTime.UtcNow.AddHours(7);var start=new DateTime(2024,1,1);var end=new DateTime(today.Year,today.Month,1);
            for(var date=start;date<=end;date=date.AddMonths(1)){
                var month=(date.Year-2024)*12+date.Month-1;
                for(int batch=0;batch<4;batch++){
                    var tag=$"{date:yyyyMM}-{batch}";var rows=sellable.Skip((batch%3)*22).Take(22).Select(v=>new PurchaseLineInput(v.Id,month==0?12:4+month%5,decimal.Round(v.Price*(0.67m+batch*0.03m),0))).ToList();
                    if(!progress.Purchases.TryGetValue(tag,out var id)){id=await Use(s=>s.SavePurchase(admin,0,new(suppliers[(month+batch)%4],rows)));progress.Purchases[tag]=id;await Save();}
                    var receipt=await Use(s=>s.GetPurchase(admin,id));if(batch<3&&receipt.Status=="NHAP")await Use(s=>s.ConfirmPurchase(admin,id,new(receipt.Version,Key("purchase:"+tag))));
                    await using var db=Open();var p=await db.Purchases.FindAsync(id);p!.CreatedAt=At(date.Year,date.Month,batch<3?batch+1:20);p.ConfirmedAt=batch<3?p.CreatedAt.AddHours(1):null;await db.Movements.Where(x=>x.PurchaseId==id).ExecuteUpdateAsync(s=>s.SetProperty(x=>x.At,p.CreatedAt.AddHours(1)));await db.SaveChangesAsync();
                }
                var last=date==end;for(int n=0;n<(last?14:12);n++){
                    var requestKey=Key($"order:{date:yyyyMM}:{n}");long id;await using(var db=Open())id=await db.Orders.Where(x=>x.RequestKey==requestKey).Select(x=>x.Id).FirstOrDefaultAsync();
                    var customer=customers[(month*7+n)%40];var v=sellable[(month*13+n*3)%sellable.Count];var second=sellable[(month*13+n*3+1)%sellable.Count];
                    if(id==0){await Use(s=>s.SetCart(customer,new(v.Id,1+n%2)));if(n%3==0)await Use(s=>s.SetCart(customer,new(second.Id,1)));var cart=await Use(s=>s.GetCart(customer));id=await Use(s=>s.PlaceOrder(customer,new(requestKey,"Người nhận mẫu "+(month*14+n+1),"0989000000","Địa chỉ nhận hàng mẫu, Quận 3, TP. Hồ Chí Minh",cart.Lines.Select(x=>new PriceConfirmation(x.VariantId,x.Quantity,x.Price)).ToList())));}
                    if(!progress.Orders.Contains(id)){progress.Orders.Add(id);await Save();}
                    var target=n switch{8=>"DA_HUY",9=>"RETURNED",10=>last?"GIAO_THAT_BAI":"RETURNED",11=>last?"CHO_XAC_NHAN":"HOAN_TAT",12=>"DA_XAC_NHAN",13=>"DANG_GIAO",_=>"HOAN_TAT"};
                    async Task Move(string action){var o=await Use(s=>s.GetOrder(admin,id,true));await Use(s=>s.Transition(admin,id,action,new(o.Version,Key($"move:{id}:{action}"),"Giao dịch mẫu theo kỳ báo cáo"),true));}
                    var current=await Use(s=>s.GetOrder(admin,id,true));if(target=="DA_HUY"&&current.Status=="CHO_XAC_NHAN")await Move("cancel");
                    else if(target!="DA_HUY"&&target!="CHO_XAC_NHAN"){
                        if(current.Status=="CHO_XAC_NHAN")await Move("confirm");current=await Use(s=>s.GetOrder(admin,id,true));
                        if(target!="DA_XAC_NHAN"&&current.Status=="DA_XAC_NHAN")await Move("dispatch");current=await Use(s=>s.GetOrder(admin,id,true));
                        if(target is "HOAN_TAT" or "RETURNED" or "GIAO_THAT_BAI"&&current.Status=="DANG_GIAO")await Move(target=="HOAN_TAT"?"complete":"fail");
                        current=await Use(s=>s.GetOrder(admin,id,true));if(target=="RETURNED"&&!current.ReturnReceived)await Move("return");
                    }
                    await using(var db=Open()){
                        var order=await db.Orders.Include(x=>x.Payment).Include(x=>x.History).SingleAsync(x=>x.Id==id);var created=At(date.Year,date.Month,5+n);order.CreatedAt=created;order.CompletedAt=order.Status=="HOAN_TAT"?created.AddDays(1):null;order.Payment.CollectedAt=order.CompletedAt;
                        var histories=order.History.OrderBy(x=>x.At).ThenBy(x=>x.Id).ToList();for(int h=0;h<histories.Count;h++)histories[h].At=created.AddHours(h*2);
                        var movements=await db.Movements.Where(x=>x.OrderId==id).ToListAsync();foreach(var movement in movements)movement.At=created.AddHours(movement.Kind switch{"GIU"=>0,"HUY_GIU"=>2,"XUAT_GIAO"=>4,"NHAN_LAI"=>8,_=>0});await db.SaveChangesAsync();
                    }
                }
                Console.WriteLine($"DEMO {date:yyyy-MM}: đã tạo phiếu nhập và đơn hàng.");
            }
            progress.Complete=true;await Save();
        }
        await using(var db=Open()){
            var stocks=await db.Stock.ToListAsync();var movements=await db.Movements.ToListAsync();foreach(var stock in stocks){if(stock.OnHand!=movements.Where(x=>x.VariantId==stock.VariantId).Sum(x=>x.DeltaOnHand)||stock.Reserved!=movements.Where(x=>x.VariantId==stock.VariantId).Sum(x=>x.DeltaReserved)||stock.OnHand<stock.Reserved||stock.Reserved<0)throw new Exception("Inventory reconciliation failed");}
            var held=await db.OrderLines.Where(x=>x.Order.Status=="CHO_XAC_NHAN"||x.Order.Status=="DA_XAC_NHAN").GroupBy(x=>x.VariantId).Select(g=>new{Id=g.Key,Quantity=g.Sum(x=>x.Quantity)}).ToDictionaryAsync(x=>x.Id,x=>x.Quantity);if(stocks.Any(x=>x.Reserved!=held.GetValueOrDefault(x.VariantId)))throw new Exception("Reserved stock vs open orders mismatch");
            var purchaseIds=progress.Purchases.Values.ToArray();var purchases=await db.Purchases.Include(x=>x.Lines).Where(x=>purchaseIds.Contains(x.Id)).ToListAsync();foreach(var p in purchases){if(p.Total!=p.Lines.Sum(l=>l.Cost*l.Quantity))throw new Exception("Purchase total mismatch");foreach(var line in p.Lines){var received=movements.Where(x=>x.PurchaseId==p.Id&&x.VariantId==line.VariantId).Sum(x=>x.DeltaOnHand);if(received!=(p.Status=="DA_XAC_NHAN"?line.Quantity:0))throw new Exception("Receipt stock reconciliation failed");}}
            var orders=await db.Orders.Include(x=>x.Payment).Include(x=>x.Lines).Include(x=>x.History).Where(x=>progress.Orders.Contains(x.Id)).ToListAsync();if(orders.Any(x=>x.Total!=x.Lines.Sum(l=>l.Price*l.Quantity)||x.Payment.Status=="DA_THU"&&(x.Status!="HOAN_TAT"||x.Payment.Amount!=x.Total)))throw new Exception("Order/payment reconciliation failed");
            foreach(var order in orders)foreach(var history in order.History.Where(x=>x.To=="HOAN_TAT"))history.At=order.CompletedAt!.Value;
            await db.SaveChangesAsync();var demoCustomers=orders.Select(x=>x.CustomerId).Distinct().ToArray();await db.Accounts.Where(x=>demoCustomers.Contains(x.Id)&&x.Username.StartsWith("demo.customer")).ExecuteUpdateAsync(s=>s.SetProperty(x=>x.CreatedAt,At(2023,12,20)));
            var reportChecks=new List<object>();
            async Task VerifyPeriod(string period,DateOnly from,DateOnly to){var result=await Use(s=>s.Report(admin,"revenue",from,to));var lo=from.ToDateTime(TimeOnly.MinValue).AddHours(-7);var hi=to.AddDays(1).ToDateTime(TimeOnly.MinValue).AddHours(-7);var expected=await db.Orders.Where(x=>x.Status=="HOAN_TAT"&&x.Payment.Status=="DA_THU"&&x.CompletedAt>=lo&&x.CompletedAt<hi).SumAsync(x=>x.Total);if(result.Total!=expected)throw new Exception("Revenue period mismatch: "+period);reportChecks.Add(new{period,from,to,total=expected,status="PASS"});}
            var localToday=DateTime.UtcNow.AddHours(7);for(int year=2024;year<=localToday.Year;year++){await VerifyPeriod("Year "+year,new(year,1,1),new(year,12,31));for(int q=1;q<=4;q++){var first=new DateOnly(year,(q-1)*3+1,1);await VerifyPeriod($"{year}-Q{q}",first,first.AddMonths(3).AddDays(-1));}for(int m=1;m<=12&&(year<localToday.Year||m<=localToday.Month);m++){var first=new DateOnly(year,m,1);await VerifyPeriod($"{year}-{m:D2}",first,first.AddMonths(1).AddDays(-1));}}
            await File.WriteAllTextAsync(Path.Combine(folder,"report-verification.json"),JsonSerializer.Serialize(reportChecks,json));
            var paid=orders.Where(x=>x.Status=="HOAN_TAT"&&x.Payment.Status=="DA_THU").ToList();
            var report=new{database=state.Database,products=await db.Products.CountAsync(x=>x.Code.StartsWith("DEMO-P")),variants=await db.Variants.CountAsync(x=>x.Sku.StartsWith("DEMO-P")),customers=await db.Accounts.CountAsync(x=>x.Username.StartsWith("demo.customer")),purchases=progress.Purchases.Count,orders=orders.Count,statuses=orders.GroupBy(x=>x.Status).Select(g=>new{status=g.Key,count=g.Count()}),monthly=paid.GroupBy(x=>x.CompletedAt!.Value.AddHours(7).ToString("yyyy-MM")).OrderBy(g=>g.Key).Select(g=>new{month=g.Key,orders=g.Count(),revenue=g.Sum(x=>x.Total)}),quarterly=paid.GroupBy(x=>new{x.CompletedAt!.Value.AddHours(7).Year,Quarter=(x.CompletedAt.Value.AddHours(7).Month-1)/3+1}).OrderBy(g=>g.Key.Year).ThenBy(g=>g.Key.Quarter).Select(g=>new{g.Key.Year,g.Key.Quarter,revenue=g.Sum(x=>x.Total)}),yearly=paid.GroupBy(x=>x.CompletedAt!.Value.AddHours(7).Year).OrderBy(g=>g.Key).Select(g=>new{year=g.Key,revenue=g.Sum(x=>x.Total)}),verification="PASS: quantities, journal, order totals and paid/completed revenue"};
            await File.WriteAllTextAsync(Path.Combine(folder,"summary.json"),JsonSerializer.Serialize(report,json));Console.WriteLine($"DEMO PASS: {orders.Count} đơn, {progress.Purchases.Count} phiếu nhập; chạy lại không nhân đôi.");
        }
    }
}
