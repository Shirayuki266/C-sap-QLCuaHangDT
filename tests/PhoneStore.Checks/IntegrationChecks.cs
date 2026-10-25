using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using PhoneStore.Core.Contracts;
using PhoneStore.Core.Models;
using PhoneStore.Infrastructure.Data;
using PhoneStore.Infrastructure.Services;

internal static class IntegrationChecks
{
    public static async Task Run()
    {
        var configured=Environment.GetEnvironmentVariable("PHONESTORE_TEST_CONNECTION");
        QaTarget.Validate(configured,Environment.GetEnvironmentVariable("PHONESTORE_QA_SERVER"),Environment.GetEnvironmentVariable("PHONESTORE_QA_DATABASE"));
        var builder=new SqlConnectionStringBuilder(configured) {InitialCatalog="PhoneStore_QA_Integration_"+DateTime.UtcNow.ToString("yyyyMMdd_HHmmss")+"_"+Guid.NewGuid().ToString("N")[..5]};
        QaTarget.Validate(builder.ConnectionString,Environment.GetEnvironmentVariable("PHONESTORE_QA_SERVER"),builder.InitialCatalog);
        var options=new DbContextOptionsBuilder<StoreDbContext>().UseSqlServer(builder.ConnectionString).Options;
        StoreDbContext Open()=>new(options);
        async Task<T> Use<T>(Func<StoreService,Task<T>> action){await using var db=Open();return await action(new(db));}
        var count=0;
        void Assert(bool value,string? message=null){if(!value)throw new Exception(message??"Assertion failed");}
        async Task Reject(string code,Func<Task> action){try{await action();}catch(BusinessException ex)when(ex.Code==code){return;}throw new Exception("Expected "+code);}
        async Task Check(string name,Func<Task> action){await action();count++;Console.WriteLine("PASS SQL "+name);}
        Console.WriteLine("Integration database: "+builder.InitialCatalog);
        await using(var db=Open()){await DatabaseSetup.Migrate(db);await DatabaseSetup.Migrate(db);}
        await Use(s=>s.Seed("Admin@12345","Khach@12345"));
        long admin,customer;int brand,category;
        await using(var db=Open()){admin=(await db.Accounts.SingleAsync(x=>x.Role=="ADMIN")).Id;customer=(await db.Customers.SingleAsync()).Id;brand=(await db.Brands.FirstAsync()).Id;category=(await db.Categories.FirstAsync()).Id;}
        var other=await Use(s=>s.Register(new("other","Other@12345","Khách khác","0912345678","other@example.test","Địa chỉ người mua khác")));
        var sequence=0;
        async Task<List<VariantDto>> Create(int amount,int variants=1){var n=++sequence;var id=await Use(s=>s.SaveProduct(admin,0,new($"TEST{n}",$"Điện thoại kiểm thử {n}",brand,category,"Cấu hình thử","Mô tả",null,true,Enumerable.Range(1,variants).Select(i=>new VariantInput(0,$"TEST{n}-{i}",$"Màu {i}",128,1000000m,true)).ToList())));var p=await Use(s=>s.ProductDetail(id,true,admin));if(amount>0){var supplier=(await Use(s=>s.Suppliers(admin))).First();var purchase=await Use(s=>s.SavePurchase(admin,0,new(supplier.Id,p.Variants.Select(v=>new PurchaseLineInput(v.Id,amount,500000m)).ToList())));var receipt=await Use(s=>s.GetPurchase(admin,purchase));await Use(s=>s.ConfirmPurchase(admin,purchase,new(receipt.Version,Guid.NewGuid())));}return (await Use(s=>s.ProductDetail(id,true,admin))).Variants;}
        async Task<CheckoutRequest> Cart(long who,IEnumerable<VariantDto> variants){foreach(var v in variants)await Use(s=>s.SetCart(who,new(v.Id,1)));var cart=await Use(s=>s.GetCart(who));return new(Guid.NewGuid(),"Người nhận","0909999999","123 Nguyễn Trãi, Quận 1",cart.Lines.Select(x=>new PriceConfirmation(x.VariantId,x.Quantity,x.Price)).ToList());}
        async Task Move(long id,string action,string? reason=null){var order=await Use(s=>s.GetOrder(admin,id,true));await Use(s=>s.Transition(admin,id,action,new(order.Version,Guid.NewGuid(),reason),true));}
        await Check("registration, password hash, duplicate input and login roles",async()=>{
            await Reject("INVALID_INPUT",async()=>await Use(s=>s.Register(new("bad","short","Tên","invalid",null,null))));
            await Reject("INVALID_INPUT",async()=>await Use(s=>s.Register(new("other","Other@12345","Trùng","0988888888",null,null))));
            await Reject("INVALID_INPUT",async()=>await Use(s=>s.Register(new("duplicatephone","Other@12345","Trùng","0912345678",null,null))));
            await Reject("UNAUTHORIZED",async()=>await Use(s=>s.Login(new("other","wrong"))));
            await Reject("UNAUTHORIZED",async()=>await Use(s=>s.Login(new("other","Other@12345"),true)));
            var login=await Use(s=>s.Login(new("other","Other@12345")));Assert(login.User.Id==other);
            await using var db=Open();Assert((await db.Accounts.FindAsync(other))!.PasswordHash!="Other@12345");
        });
        await Check("catalog filters, pagination and invalid range",async()=>{
            var results=await Use(s=>s.Catalog(new(BrandId:brand,MinPrice:1,MaxPrice:30000000,StorageGb:128,Sort:"price-asc",PageSize:1)));Assert(results.Items.Count==1&&results.Total>=1);
            await Reject("INVALID_INPUT",async()=>await Use(s=>s.Catalog(new(MinPrice:10,MaxPrice:1))));
        });
        await Check("product creation starts at zero; customer cannot write catalog",async()=>{
            var variants=await Create(0);Assert(variants[0].OnHand==0);
            await Reject("FORBIDDEN",async()=>await Use(s=>s.SaveLookup(customer,"brands",0,new("Sai quyền",true))));
        });
        await Check("receipt draft, edit, confirm and repeated confirmation",async()=>{
            var v=(await Create(0))[0];var supplier=(await Use(s=>s.Suppliers(admin))).First();
            var id=await Use(s=>s.SavePurchase(admin,0,new(supplier.Id,[new(v.Id,2,700000m)])));var receipt=await Use(s=>s.GetPurchase(admin,id));Assert(receipt.Total==1400000m);
            Assert((await Use(s=>s.Stock(admin))).Single(x=>x.VariantId==v.Id).OnHand==0);
            await Use(s=>s.SavePurchase(admin,id,new(supplier.Id,[new(v.Id,3,600000m)],receipt.Version)));receipt=await Use(s=>s.GetPurchase(admin,id));
            await Use(s=>s.ConfirmPurchase(admin,id,new(receipt.Version,Guid.NewGuid())));
            var current=await Use(s=>s.GetPurchase(admin,id));await Reject("INVALID_STATE",async()=>await Use(s=>s.ConfirmPurchase(admin,id,new(current.Version,Guid.NewGuid()))));
            Assert((await Use(s=>s.Stock(admin))).Single(x=>x.VariantId==v.Id).OnHand==3);
            await Reject("INVALID_STATE",async()=>await Use(s=>s.SavePurchase(admin,id,new(supplier.Id,[],current.Version))));
        });
        await Check("cart add/update/remove and guest merge do not reserve",async()=>{
            var v=(await Create(4))[0];await Use(s=>s.SetCart(customer,new(v.Id,1),true));await Use(s=>s.MergeCart(customer,[new(v.Id,1)]));Assert((await Use(s=>s.GetCart(customer))).Lines.Single(x=>x.VariantId==v.Id).Quantity==2);
            Assert((await Use(s=>s.Stock(admin))).Single(x=>x.VariantId==v.Id).Reserved==0);await Use(s=>s.SetCart(customer,new(v.Id,0)));
            await Reject("OUT_OF_STOCK",async()=>await Use(s=>s.SetCart(customer,new(v.Id,5))));
        });
        await Check("checkout, retry RequestKey, ownership and customer cancellation",async()=>{
            var v=await Create(2);var request=await Cart(customer,v);var id=await Use(s=>s.PlaceOrder(customer,request));Assert(await Use(s=>s.PlaceOrder(customer,request))==id);
            await Reject("FORBIDDEN",async()=>await Use(s=>s.PlaceOrder(other,request)));await Reject("NOT_FOUND",async()=>await Use(s=>s.GetOrder(other,id)));
            var order=await Use(s=>s.GetOrder(customer,id));Assert(order.Status=="CHO_XAC_NHAN"&&order.Payment=="CHUA_THU");
            Assert((await Use(s=>s.Stock(admin))).Single(x=>x.VariantId==v[0].Id).Reserved==1);
            await Use(s=>s.Transition(customer,id,"cancel",new(order.Version,Guid.NewGuid(),"Đổi nhu cầu"),false));Assert((await Use(s=>s.Stock(admin))).Single(x=>x.VariantId==v[0].Id).Reserved==0);
        });
        long completed=0;
        await Check("complete COD updates balances, history and revenue",async()=>{
            var v=await Create(2);var checkout=await Cart(customer,v);completed=await Use(s=>s.PlaceOrder(customer,checkout));
            await Move(completed,"confirm");var confirmed=await Use(s=>s.GetOrder(customer,completed));
            await Reject("INVALID_STATE",async()=>await Use(s=>s.Transition(customer,completed,"cancel",new(confirmed.Version,Guid.NewGuid(),"Hủy trễ"),false)));
            await Move(completed,"dispatch");var stock=(await Use(s=>s.Stock(admin))).Single(x=>x.VariantId==v[0].Id);Assert(stock.OnHand==1&&stock.Reserved==0);
            await Move(completed,"complete");var order=await Use(s=>s.GetOrder(customer,completed));Assert(order.Status=="HOAN_TAT"&&order.Payment=="DA_THU"&&order.History.Count==4);
        });
        await Check("failed delivery does not restock until physical return; return once",async()=>{
            var v=await Create(1);var request=await Cart(customer,v);var id=await Use(s=>s.PlaceOrder(customer,request));await Move(id,"confirm");await Move(id,"dispatch");await Move(id,"fail","Không liên lạc được");
            Assert((await Use(s=>s.Stock(admin))).Single(x=>x.VariantId==v[0].Id).OnHand==0);await Move(id,"return");Assert((await Use(s=>s.Stock(admin))).Single(x=>x.VariantId==v[0].Id).OnHand==1);
            var order=await Use(s=>s.GetOrder(customer,id));await Reject("INVALID_STATE",async()=>await Use(s=>s.Transition(admin,id,"return",new(order.Version,Guid.NewGuid()),true)));
        });
        await Check("price changes require reconfirmation; old order snapshot unchanged",async()=>{
            var variants=await Create(2);var v=variants[0];var request=await Cart(customer,variants);long productId;
            await using(var db=Open())productId=(await db.Variants.FindAsync(v.Id))!.ProductId;
            var product=await Use(s=>s.ProductDetail(productId,true,admin));
            ProductInput Update(decimal price)=>new(product.Code,"Tên mới",product.BrandId,product.CategoryId,product.Specifications,product.Description,product.ImageUrl,true,product.Variants.Select(x=>new VariantInput(x.Id,x.Sku,x.Color,x.StorageGb,price,true,x.Version)).ToList());
            await Use(s=>s.SaveProduct(admin,productId,Update(1100000m)));await Reject("PRICE_CHANGED",async()=>await Use(s=>s.PlaceOrder(customer,request)));
            request=await Cart(customer,variants);var id=await Use(s=>s.PlaceOrder(customer,request));product=await Use(s=>s.ProductDetail(productId,true,admin));await Use(s=>s.SaveProduct(admin,productId,Update(1200000m)));var order=await Use(s=>s.GetOrder(customer,id));Assert(order.Total==1100000m&&order.Lines[0].Name=="Tên mới");await Move(id,"cancel","Kết thúc kiểm thử");
        });
        await Check("last unit race across separate SQL connections",async()=>{
            var v=await Create(1);var one=await Cart(customer,v);var two=await Cart(other,v);
            async Task<bool> Try(long who,CheckoutRequest request){try{await Use(s=>s.PlaceOrder(who,request));return true;}catch(BusinessException e)when(e.Code=="OUT_OF_STOCK"){return false;}}
            var results=await Task.WhenAll(Try(customer,one),Try(other,two));Assert(results.Count(x=>x)==1);var stock=(await Use(s=>s.Stock(admin))).Single(x=>x.VariantId==v[0].Id);Assert(stock.OnHand==1&&stock.Reserved==1);
            // Remove the losing cart line before following scenarios.
            await Use(s=>s.SetCart(customer,new(v[0].Id,0)));await Use(s=>s.SetCart(other,new(v[0].Id,0)));
        });
        await Check("multi-line checkout rollback after first reservation",async()=>{
            var variants=await Create(1,2);var request=await Cart(customer,variants);var consume=await Cart(other,[variants[1]]);await Use(s=>s.PlaceOrder(other,consume));
            await Reject("OUT_OF_STOCK",async()=>await Use(s=>s.PlaceOrder(customer,request)));
            var stock=(await Use(s=>s.Stock(admin))).Single(x=>x.VariantId==variants[0].Id);Assert(stock.Reserved==0);
            await using(var db=Open()){Assert(!await db.Orders.AnyAsync(x=>x.RequestKey==request.RequestKey));Assert(!await db.Movements.AnyAsync(x=>x.OperationKey==request.RequestKey));}
            foreach(var v in variants)await Use(s=>s.SetCart(customer,new(v.Id,0)));
        });
        await Check("customer cancellation versus admin confirmation uses rowversion",async()=>{
            var variants=await Create(1);var request=await Cart(customer,variants);var id=await Use(s=>s.PlaceOrder(customer,request));var order=await Use(s=>s.GetOrder(customer,id));
            async Task<bool> Try(bool adminAction){try{await Use(s=>s.Transition(adminAction?admin:customer,id,adminAction?"confirm":"cancel",new(order.Version,Guid.NewGuid(),"Kiểm thử tranh chấp"),adminAction));return true;}catch(BusinessException e)when(e.Code=="CONCURRENT_UPDATE"){return false;}}
            var results=await Task.WhenAll(Try(true),Try(false));Assert(results.Count(x=>x)==1);
        });
        await Check("inventory adjustment requires reason and protects held stock",async()=>{
            var variants=await Create(2);var v=variants[0];var request=await Cart(customer,variants);await Use(s=>s.PlaceOrder(customer,request));var row=(await Use(s=>s.Stock(admin))).Single(x=>x.VariantId==v.Id);
            await Reject("INVALID_INPUT",async()=>await Use(s=>s.AdjustStock(admin,v.Id,new(0,"Kiểm kê",row.Version,Guid.NewGuid()))));
            await Reject("INVALID_INPUT",async()=>await Use(s=>s.AdjustStock(admin,v.Id,new(3,"",row.Version,Guid.NewGuid()))));
            await Use(s=>s.AdjustStock(admin,v.Id,new(3,"Kiểm kê thực tế",row.Version,Guid.NewGuid())));var updated=(await Use(s=>s.Stock(admin))).Single(x=>x.VariantId==v.Id);Assert(updated.OnHand==3&&updated.Reserved==1);
        });
        await Check("locked accounts lose sessions; password change revokes all",async()=>{
            var login=await Use(s=>s.Login(new("other","Other@12345")));var profile=await Use(s=>s.GetUser(other));
            await Use(s=>s.SaveCustomer(admin,other,new(profile.FullName,profile.Phone,profile.Email,profile.Address,false)));await Reject("UNAUTHORIZED",async()=>await Use(s=>s.GetCart(other)));
            await using(var db=Open())Assert(!await db.Sessions.AnyAsync(x=>x.AccountId==other));
            await Use(s=>s.SaveCustomer(admin,other,new(profile.FullName,profile.Phone,profile.Email,profile.Address,true)));await Use(s=>s.Login(new("other","Other@12345")));
            await Use(s=>s.ChangePassword(other,new("Other@12345","Changed@12345")));await Reject("UNAUTHORIZED",async()=>await Use(s=>s.Login(new("other","Other@12345"))));
            await using(var db=Open())Assert(!await db.Sessions.AnyAsync(x=>x.AccountId==other));
        });
        await Check("reports exclude unpaid/cancelled/failed and share table/chart rows",async()=>{
            var report=await Use(s=>s.Report(admin,"revenue",null,null));await using var db=Open();var expected=await db.Orders.Where(x=>x.Status=="HOAN_TAT"&&x.Payment.Status=="DA_THU").SumAsync(x=>x.Total);Assert(report.Total==expected&&expected==1000000m);
            var sales=await Use(s=>s.Report(admin,"bestsellers",null,null));Assert(sales.Total==1);var empty=await Use(s=>s.Report(admin,"revenue",new DateOnly(2000,1,1),new DateOnly(2000,1,2)));Assert(empty.Total==0&&empty.Rows.Count==0);
            await Reject("INVALID_INPUT",async()=>await Use(s=>s.Report(admin,"revenue",new DateOnly(2026,2,1),new DateOnly(2026,1,1))));
            Assert((await Use(s=>s.Report(admin,"stock",null,null))).Rows.Count>0);
        });
        await Check("lookup, supplier and product soft CRUD preserve references",async()=>{
            var lookup=await Use(s=>s.SaveLookup(admin,"brands",0,new("Hãng nghiệm thu",true)));
            await Use(s=>s.SaveLookup(admin,"brands",lookup,new("Hãng nghiệm thu sửa",false)));Assert((await Use(s=>s.Lookups("brands",true,admin))).Single(x=>x.Id==lookup).Active==false);
            var supplier=await Use(s=>s.SaveSupplier(admin,0,new("TESTSUP","Nhà cung cấp thử","0922222222","Địa chỉ NCC",true)));
            await Use(s=>s.SaveSupplier(admin,supplier,new("TESTSUP","NCC đã sửa",null,null,false)));
            var v=(await Create(0))[0];await Reject("INVALID_INPUT",async()=>await Use(s=>s.SavePurchase(admin,0,new(supplier,[new(v.Id,1,1)]))));
            long productId;await using(var db=Open())productId=(await db.Variants.FindAsync(v.Id))!.ProductId;
            var product=await Use(s=>s.ProductDetail(productId,true,admin));var input=new ProductInput(product.Code,product.Name,product.BrandId,product.CategoryId,product.Specifications,product.Description,product.ImageUrl,false,product.Variants.Select(x=>new VariantInput(x.Id,x.Sku,x.Color,x.StorageGb,x.Price,x.Active,x.Version)).ToList());
            await Use(s=>s.SaveProduct(admin,product.Id,input));await Reject("NOT_FOUND",async()=>await Use(s=>s.ProductDetail(product.Id)));
            await Reject("CONCURRENT_UPDATE",async()=>await Use(s=>s.SaveProduct(admin,product.Id,input)));
        });
        await Check("empty/cancelled receipts and nonpositive cost rejected",async()=>{
            var v=(await Create(0))[0];var supplier=(await Use(s=>s.Suppliers(admin))).First(x=>x.Active);
            await Reject("INVALID_INPUT",async()=>await Use(s=>s.SavePurchase(admin,0,new(supplier.Id,[new(v.Id,1,0)]))));
            var id=await Use(s=>s.SavePurchase(admin,0,new(supplier.Id,[])));var p=await Use(s=>s.GetPurchase(admin,id));
            await Reject("INVALID_INPUT",async()=>await Use(s=>s.ConfirmPurchase(admin,id,new(p.Version,Guid.NewGuid()))));
            await Use(s=>s.ConfirmPurchase(admin,id,new(p.Version,Guid.NewGuid()),true));var cancelled=await Use(s=>s.GetPurchase(admin,id));Assert(cancelled.Status=="DA_HUY");
            await Reject("INVALID_STATE",async()=>await Use(s=>s.ConfirmPurchase(admin,id,new(cancelled.Version,Guid.NewGuid()))));
        });
        await Check("profile uniqueness and mutation validation",async()=>{
            var before=await Use(s=>s.GetUser(customer));
            await Reject("INVALID_INPUT",async()=>await Use(s=>s.UpdateProfile(customer,new(before.FullName,"0912345678",before.Email,before.Address))));
            await Use(s=>s.UpdateProfile(customer,new("Họ tên cập nhật",before.Phone,"updated@example.test","Địa chỉ cập nhật mặc định")));
            Assert((await Use(s=>s.GetUser(customer))).FullName=="Họ tên cập nhật");
            await Reject("FORBIDDEN",async()=>await Use(s=>s.Report(customer,"revenue",null,null)));
        });
        await Check("report periods use UTC+7 and exclusive next-day boundary",async()=>{
            DateTime original;await using(var db=Open()){original=(await db.Orders.FindAsync(completed))!.CompletedAt!.Value;var boundary=new DateTime(2020,5,15,17,0,0,DateTimeKind.Utc);await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE DON_HANG SET CompletedAt={boundary} WHERE Id={completed}");}
            try{Assert((await Use(s=>s.Report(admin,"revenue",new(2020,5,15),new(2020,5,15)))).Total==0);Assert((await Use(s=>s.Report(admin,"revenue",new(2020,5,16),new(2020,5,16)))).Total==1000000m);}
            finally{await using var db=Open();await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE DON_HANG SET CompletedAt={original} WHERE Id={completed}");}
        });
        await Check("audit journal reconciles all inventory and COD totals",async()=>{
            await using var db=Open();var stocks=await db.Stock.ToListAsync();var movements=await db.Movements.ToListAsync();
            foreach(var s in stocks){Assert(s.OnHand==movements.Where(x=>x.VariantId==s.VariantId).Sum(x=>x.DeltaOnHand));Assert(s.Reserved==movements.Where(x=>x.VariantId==s.VariantId).Sum(x=>x.DeltaReserved));Assert(s.Reserved>=0&&s.OnHand>=s.Reserved);}
            var paid=await db.Payments.Include(x=>x.Order).Where(x=>x.Status=="DA_THU").ToListAsync();Assert(paid.All(x=>x.Amount==x.Order.Total&&x.ActorId==admin&&x.Order.Status=="HOAN_TAT"));
        });
        Console.WriteLine($"{count} SQL integration checks passed. Database retained: {builder.InitialCatalog}");
    }
}


