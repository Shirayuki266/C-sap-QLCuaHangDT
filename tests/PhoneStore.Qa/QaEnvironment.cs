using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using PhoneStore.Infrastructure.Data;
using PhoneStore.Infrastructure.Services;
using PhoneStore.Core.Contracts;
using System.Text.Json;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
internal static class QaEnvironment
{
    internal sealed record Settings(string Server,string ConnectionString,string DatabasePrefix,string Environment);
    internal sealed record State(string Server,string Database,string Url,string AdminUsername,string CustomerUsername,string Customer2Username,string AdminPassword,string CustomerPassword,string ConnectionString);
    static readonly JsonSerializerOptions Json=new(){PropertyNamingPolicy=JsonNamingPolicy.CamelCase,PropertyNameCaseInsensitive=true,WriteIndented=true};
    internal static Settings Config(string root)=>JsonSerializer.Deserialize<Settings>(File.ReadAllText(Path.Combine(root,"qa/settings.json")),Json)!;
    internal static SqlConnectionStringBuilder Connection(string root,string database)
    {
        var config=Config(root);var cs=new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("PHONESTORE_TEST_CONNECTION")??config.ConnectionString){InitialCatalog=database};
        QaTarget.Validate(cs.ConnectionString,config.Server,database);return cs;
    }
    internal static string NewDatabase()=>"PhoneStore_QA_"+DateTime.UtcNow.ToString("yyyyMMdd_HHmmss")+"_"+Guid.NewGuid().ToString("N")[..6];
    public static async Task Run(string root,string action)
    {
        var folder=Path.Combine(root,".artifacts/qa-env");Directory.CreateDirectory(folder);var file=Path.Combine(folder,"private.json");
        if(action=="--init"){
            if(File.Exists(file)){Console.WriteLine("QA đã khởi tạo; không seed/reset lại. Dùng --serve hoặc --cleanup rõ ràng.");return;}
            var cs=Connection(root,NewDatabase());var listener=new TcpListener(IPAddress.Loopback,0);listener.Start();var port=((IPEndPoint)listener.LocalEndpoint).Port;listener.Stop();
            var state=new State(cs.DataSource,cs.InitialCatalog,"http://localhost:"+port,"admin","khachhang","qa-second","QaA1!"+Guid.NewGuid().ToString("N"),"QaC1!"+Guid.NewGuid().ToString("N"),cs.ConnectionString);
            await using var db=new StoreDbContext(new DbContextOptionsBuilder<StoreDbContext>().UseSqlServer(cs.ConnectionString).Options);
            await File.AppendAllTextAsync(Path.Combine(folder,"attempts.log"),cs.InitialCatalog+System.Environment.NewLine); await DatabaseSetup.Migrate(db);var service=new StoreService(db);await service.Seed(state.AdminPassword,state.CustomerPassword);
            await service.Register(new(state.Customer2Username,state.CustomerPassword,"Khách QA thứ hai","0907654321","second@example.test","Địa chỉ kiểm thử"));
            var admin=(await db.Accounts.SingleAsync(x=>x.Role=="ADMIN")).Id;var customer=(await db.Accounts.SingleAsync(x=>x.Username==state.CustomerUsername)).Id;
            var brand=(await db.Brands.FirstAsync()).Id;var category=(await db.Categories.FirstAsync()).Id;var supplier=(await service.Suppliers(admin)).First().Id;
            var product=await service.SaveProduct(admin,0,new("QA-FIXTURE","Điện thoại QA nhiều biến thể",brand,category,"Cấu hình QA","Dữ liệu hoàn toàn giả lập dùng kiểm thử",null,true,[new(0,"QA-ZERO","Đen",64,100000m,true),new(0,"QA-LAST","Xanh",128,200000m,true),new(0,"QA-MANY","Đỏ",256,300000m,true)]));
            var variants=(await service.ProductDetail(product,true,admin)).Variants;
            var receipt=await service.SavePurchase(admin,0,new(supplier,[new(variants.Single(x=>x.Sku=="QA-LAST").Id,1,150000m),new(variants.Single(x=>x.Sku=="QA-MANY").Id,30,250000m)]));
            var purchase=await service.GetPurchase(admin,receipt);await service.ConfirmPurchase(admin,receipt,new(purchase.Version,Guid.NewGuid()));
            await service.SavePurchase(admin,0,new(supplier,[new(variants[0].Id,3,80000m)]));
            foreach(var target in new[]{"CHO_XAC_NHAN","DA_XAC_NHAN","DANG_GIAO","HOAN_TAT","GIAO_THAT_BAI","DA_HUY"}){
                db.ChangeTracker.Clear();var v=variants.Single(x=>x.Sku=="QA-MANY");await service.SetCart(customer,new(v.Id,1));
                var id=await service.PlaceOrder(customer,new(Guid.NewGuid(),"Người nhận QA","0909999999","123 địa chỉ QA",[new(v.Id,1,v.Price)]));
                async Task Move(string move){db.ChangeTracker.Clear();var order=await service.GetOrder(admin,id,true);await service.Transition(admin,id,move,new(order.Version,Guid.NewGuid(),"Fixture QA"),true);}
                if(target=="DA_HUY"){await Move("cancel");continue;}
                if(target=="CHO_XAC_NHAN")continue;await Move("confirm");if(target=="DA_XAC_NHAN")continue;
                await Move("dispatch");if(target=="DANG_GIAO")continue;await Move(target=="HOAN_TAT"?"complete":"fail");
            }
            await File.WriteAllTextAsync(file,JsonSerializer.Serialize(state,Json));
            await File.WriteAllTextAsync(Path.Combine(folder,"environment.json"),JsonSerializer.Serialize(new{server=state.Server,database=state.Database,url=state.Url,environment="QA",credentials=".artifacts/qa-env/private.json (local only; do not publish)"},Json));
            Console.WriteLine("QA ready: "+state.Database+" "+state.Url+"; tài khoản trong tệp private.json cục bộ, không đưa vào báo cáo.");return;
        }
        if(!File.Exists(file))throw new InvalidOperationException("Chưa init QA; chạy --init trước.");
        var current=JsonSerializer.Deserialize<State>(await File.ReadAllTextAsync(file),Json)!;
        QaTarget.Validate(current.ConnectionString,Config(root).Server,current.Database);
        if(action=="--verify"){
            await using var db=new StoreDbContext(new DbContextOptionsBuilder<StoreDbContext>().UseSqlServer(current.ConnectionString).Options);
            var stock=await db.Stock.Where(x=>x.Variant.Sku.StartsWith("QA-")).OrderBy(x=>x.Variant.Sku).Select(x=>new{x.Variant.Sku,x.OnHand,x.Reserved}).ToListAsync();
            var states=await db.Orders.Where(x=>x.Lines.Any(l=>l.Sku=="QA-MANY")).GroupBy(x=>x.Status).Select(x=>new{Status=x.Key,Count=x.Count()}).ToListAsync();
            var valid=stock.Count==3&&stock.Single(x=>x.Sku=="QA-ZERO").OnHand==0&&stock.Single(x=>x.Sku=="QA-LAST").OnHand==1&&stock.Single(x=>x.Sku=="QA-MANY").OnHand==27&&stock.Single(x=>x.Sku=="QA-MANY").Reserved==2&&states.Count==6&&states.All(x=>x.Count==1)&&await db.Purchases.AnyAsync(x=>x.Status=="NHAP")&&await db.Purchases.AnyAsync(x=>x.Status=="DA_XAC_NHAN")&&await db.Customers.CountAsync()>=2;
            await File.WriteAllTextAsync(Path.Combine(folder,"verification.json"),JsonSerializer.Serialize(new{database=current.Database,stock,states,status=valid?"PASS":"FAIL",expected="Fixture gốc 0/1/27 thực, giữ0/0/2, sáu trạng thái đơn, phiếu nháp/chốt, hai khách",note="Kiểm tra fixture ban đầu; sau khi thao tác dữ liệu bằng tay, số dư có thể khác và cần đối chiếu nghiệp vụ."},Json));
            if(!valid)throw new InvalidOperationException("Fixture QA khác số dư gốc; xem verification.json, không reset tự động.");Console.WriteLine("PASS QA fixture SQL verification");return;
        }
        if(action=="--cleanup"){
            var cs=new SqlConnectionStringBuilder(current.ConnectionString){InitialCatalog="master"};await using var sql=new SqlConnection(cs.ConnectionString);await sql.OpenAsync();
            await using var cmd=sql.CreateCommand();cmd.CommandText="DECLARE @sql nvarchar(max)=N'DROP DATABASE '+QUOTENAME(@db); EXEC(@sql);";cmd.Parameters.AddWithValue("@db",current.Database);await cmd.ExecuteNonQueryAsync();
            File.Delete(file);File.Delete(Path.Combine(folder,"environment.json"));Console.WriteLine("Đã xóa đúng DB QA trong manifest; không xóa DB khác.");return;
        }
        if(action!="--serve")throw new InvalidOperationException("Unknown QA action");
        var info=new ProcessStartInfo("dotnet"){WorkingDirectory=Path.Combine(root,"src/PhoneStore.Web"),UseShellExecute=false};info.ArgumentList.Add(Path.Combine(root,"src/PhoneStore.Web/bin/Release/net10.0/PhoneStore.Web.dll"));info.ArgumentList.Add("--urls");info.ArgumentList.Add(current.Url);
        info.Environment["ASPNETCORE_ENVIRONMENT"]="QA";info.Environment["ConnectionStrings__Store"]=current.ConnectionString;info.Environment["QA__Server"]=current.Server;info.Environment["QA__Database"]=current.Database;
        using var process=Process.Start(info)!;try{await process.WaitForExitAsync();}finally{if(!process.HasExited)process.Kill(true);}Environment.ExitCode=process.ExitCode;
    }
}
