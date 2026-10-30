using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PhoneStore.Core.Commerce;
using PhoneStore.Infrastructure.Accounts;
using PhoneStore.Infrastructure.Commerce;
namespace PhoneStore.Infrastructure.Data;
public sealed class DemoSeeder(StoreDbContext db,IPasswordHasher<Account> passwords)
{
    public const string AdminUsername="demo.admin.v1";
    public async Task<DatabaseAudit> Seed(string? adminPassword,string? customerPassword)
    {
        var adminSecret=PasswordInput.Validate(adminPassword);var customerSecret=PasswordInput.Validate(customerPassword);
        if(!await new DatabaseMigrator(db).IsCurrentAsync())throw new InvalidOperationException("Chỉ tạo dữ liệu mẫu trên schema bản xây lại đã được kiểm tra.");
        var clock=new DemoClock(DateTimeOffset.UtcNow);var kernel=new BusinessKernel(db,clock);
        return await kernel.Write(async()=>
        {
            var audit=await new DataAudit(db).Run();
            if(!audit.Valid)throw new InvalidOperationException("CSDL không đạt kiểm tra trước khi tạo mẫu: "+string.Join(", ",audit.Errors));
            if(audit.Counts.Where(x=>x.Key!="__PhoneStoreMigrations").Any(x=>x.Value!=0))
            {
                if(!audit.Valid||!ExpectedCounts.All(x=>audit.Counts[x.Key]==x.Value)||await db.Accounts.CountAsync(x=>x.Username==AdminUsername||x.Username.StartsWith("demo.customer."))!=21||await db.Products.CountAsync(x=>x.Code.StartsWith("DEMO"))!=20)
                    throw new InvalidOperationException("CSDL đã có dữ liệu riêng hoặc bộ mẫu đã thay đổi. Không ghi đè, reset hoặc bổ sung dở dang.");
                return audit;
            }
            var now=clock.Current;
            var admin=new Account{Username=AdminUsername,Role="ADMIN",CreatedAt=now.AddDays(-60).UtcDateTime};admin.PasswordHash=passwords.HashPassword(admin,adminSecret);db.Add(admin);await db.SaveChangesAsync();
            var adminId=admin.Id;var adminSession=Guid.NewGuid();db.Sessions.Add(new LoginSession{Id=adminSession,AccountId=adminId,ExpiresAt=now.AddHours(8).UtcDateTime});
            var names=new[]{"Nguyễn Minh An","Trần Hoàng Bảo","Lê Ngọc Châu","Phạm Anh Dũng","Hoàng Thu Giang","Vũ Quang Hải","Đặng Minh Hằng","Bùi Quốc Huy","Đỗ Khánh Linh","Hồ Tuấn Kiệt","Ngô Phương Mai","Dương Bảo Nam","Lý Thảo Nguyên","Mai Đức Phúc","Võ Minh Quân","Trịnh Hồng Sơn","Đinh Thanh Tâm","Phan Ngọc Trang","Cao Anh Tú","Tạ Hải Yến"};
            var customers=new List<(Actor Actor,string Name,string Phone,string Address)>();
            for(var i=0;i<20;i++)
            {
                var account=new Account{Username=$"demo.customer.{i+1:00}",CreatedAt=now.AddDays(-50).UtcDateTime};account.PasswordHash=passwords.HashPassword(account,customerSecret);db.Add(account);await db.SaveChangesAsync();
                var phone=$"090100{i+1:0000}";var address=$"{i+10} Đường Mẫu, Phường Minh Họa, TP Hồ Chí Minh (dữ liệu mẫu)";
                var profile=ProfileInput.Validate(new(names[i],phone,$"customer{i+1:00}@example.test",address));
                db.Customers.Add(new Customer{Id=account.Id,FullName=profile.FullName,Phone=profile.Phone,Email=profile.Email,Address=profile.Address});
                var session=Guid.NewGuid();db.Sessions.Add(new LoginSession{Id=session,AccountId=account.Id,ExpiresAt=now.AddHours(8).UtcDateTime});customers.Add((new(account.Id,session),names[i],phone,address));
            }
            await db.SaveChangesAsync();db.ChangeTracker.Clear();var actor=new Actor(adminId,adminSession);
            var brands=new[]{"Apple","Samsung","Xiaomi","Oppo","Vivo","Realme","OnePlus","Nokia","Motorola","Sony","Asus","Google","Huawei","Honor","Tecno","Infinix","Nothing","ZTE","Nubia","Meizu"};
            var categories=new[]{"Điện thoại phổ thông","Điện thoại 5G","Điện thoại chụp ảnh","Điện thoại chơi game","Điện thoại pin lớn","Điện thoại nhỏ gọn","Điện thoại màn hình lớn","Điện thoại gập","Điện thoại văn phòng","Điện thoại học tập","Điện thoại cao cấp","Điện thoại tầm trung","Điện thoại giá rẻ","Điện thoại kháng nước","Điện thoại hai SIM","Điện thoại sạc nhanh","Điện thoại OLED","Điện thoại Android","Điện thoại iOS","Điện thoại bền bỉ"};
            var products=new List<AdminProduct>();var supplierIds=new List<long>();
            var lookups=new LookupService(kernel);var productService=new ProductService(kernel);var suppliers=new SupplierService(kernel);var purchases=new PurchaseService(kernel);
            clock.Current=now.AddDays(-40);
            for(var i=0;i<20;i++)
            {
                var brand=await lookups.Save(actor,"brands",0,new(brands[i]));db.ChangeTracker.Clear();var category=await lookups.Save(actor,"categories",0,new(categories[i]));db.ChangeTracker.Clear();
                var code=$"DEMO{i+1:000}";var cost=2000000m+i*350000m;
                var id=await productService.Save(actor,0,new(code,$"{brands[i]} Mẫu {i+1:00} (DEMO)",brand,category,"Cấu hình minh họa: màn hình 6.5 inch; RAM 8GB; pin 5000mAh. Không phải thông số hãng xác nhận.","Sản phẩm dùng thử nghiệp vụ, không phải niêm yết thương mại.","/images/demo-phone.svg",true,[new(0,code+"-128","Đen",128,cost,15+i%4),new(0,code+"-256","Xanh",256,cost+500000m,20)],[new("/images/demo-phone.svg")]));db.ChangeTracker.Clear();
                var product=await productService.Read(actor,id);products.Add(product);
                var supplier=await suppliers.Save(actor,0,new($"NCC{i+1:000}",$"Nhà cung cấp minh họa {i+1:00}",$"0289000{i+1:000}",$"{i+1} Khu phân phối mẫu, TP Hồ Chí Minh",true));db.ChangeTracker.Clear();supplierIds.Add(supplier);
                var receiptId=await purchases.Save(actor,0,new(supplier,product.Variants.Select((v,n)=>new PurchaseLineInput(v.Id,n==0?30:15,v.Cost+100000m)).ToList(),RequestKey:Guid.NewGuid()));db.ChangeTracker.Clear();
                var receipt=await purchases.Read(actor,receiptId);await purchases.Confirm(actor,receiptId,new(receipt.Version,Guid.NewGuid()));db.ChangeTracker.Clear();
                products[i]=await productService.Read(actor,id);
            }
            var cart=new CartService(kernel);var checkout=new CheckoutService(kernel);var workflow=new OrderWorkflow(kernel);var queries=new OrderQueryService(kernel);
            for(var i=0;i<20;i++)
            {
                clock.Current=now.AddDays(-20+i);var c=customers[i];var product=products[i];
                foreach(var v in product.Variants){await cart.Set(c.Actor,new(v.Id,1));db.ChangeTracker.Clear();}
                var current=await cart.Read(c.Actor);var order=await checkout.Place(c.Actor,new(Guid.NewGuid(),c.Name,c.Phone,c.Address,current.Lines.Select(x=>new PriceConfirmation(x.VariantId,x.Quantity,x.Price!.Value)).ToList()));db.ChangeTracker.Clear();
                var state=i%7;
                if(state==4){await workflow.Cancel(c.Actor,order.Id,new(order.Version,Guid.NewGuid(),"Khách đổi nhu cầu (mẫu)"));db.ChangeTracker.Clear();}
                else if(state!=0)
                {
                    await workflow.Confirm(actor,order.Id,new(order.Version,Guid.NewGuid()));db.ChangeTracker.Clear();order=await queries.Read(actor,order.Id,true);
                    if(state>=2)
                    {
                        await workflow.Dispatch(actor,order.Id,new(order.Version,Guid.NewGuid()));db.ChangeTracker.Clear();order=await queries.Read(actor,order.Id,true);
                        if(state==3){await workflow.Complete(actor,order.Id,new(order.Version,Guid.NewGuid()));db.ChangeTracker.Clear();}
                        else if(state>=5)
                        {
                            await workflow.Fail(actor,order.Id,new(order.Version,Guid.NewGuid(),"Không liên lạc được người nhận (mẫu)"));db.ChangeTracker.Clear();order=await queries.Read(actor,order.Id,true);
                            if(state==6){await workflow.ReceiveReturn(actor,order.Id,new(order.Version,Guid.NewGuid(),"Kho xác nhận nhận đủ hàng (mẫu)"));db.ChangeTracker.Clear();}
                        }
                    }
                }
                await cart.Set(c.Actor,new(product.Variants[0].Id,1));db.ChangeTracker.Clear();
            }
            clock.Current=now;var inventory=new InventoryService(kernel);var first=(await inventory.List(actor,query:products[0].Variants[0].Sku)).Items.Single();
            await inventory.Adjust(actor,first.VariantId,new(first.OnHand+2,"Kiểm kê bổ sung 2 máy minh họa",first.Version,Guid.NewGuid()));db.ChangeTracker.Clear();
            foreach(var p in products.TakeLast(2))
            {
                var current=await productService.Read(actor,p.Id);await productService.Save(actor,current.Id,new(current.Code,current.Name,current.BrandId,current.CategoryId,current.Specifications,current.Description,current.ImageUrl,false,current.Variants.Select(v=>new VariantInput(v.Id,v.Sku,v.Color,v.StorageGb,0,v.ProfitPercent,v.Active,v.Version)).ToList(),current.Images));db.ChangeTracker.Clear();
            }
            await suppliers.Save(actor,supplierIds[^1],new("NCC020","Nhà cung cấp minh họa 20","0289000020","20 Khu phân phối mẫu, TP Hồ Chí Minh",false));db.ChangeTracker.Clear();
            var result=await new DataAudit(db).Run();
            if(!result.Valid||!ExpectedCounts.All(x=>result.Counts[x.Key]==x.Value))throw new InvalidOperationException("Bộ mẫu không đạt kiểm tra: "+string.Join(", ",result.Errors));
            return result;
        });
    }
    public static readonly IReadOnlyDictionary<string,int> ExpectedCounts=new Dictionary<string,int>
    {
        ["TAI_KHOAN"]=21,["KHACH_HANG"]=20,["PHIEN"]=21,["HANG"]=20,["DANH_MUC"]=20,["SAN_PHAM"]=20,["SAN_PHAM_ANH"]=20,["BIEN_THE"]=40,["TON_KHO"]=40,["GIO_HANG_CT"]=20,
        ["DON_HANG"]=20,["DON_HANG_CT"]=40,["THANH_TOAN"]=20,["LICH_SU_DON"]=58,["NHA_CUNG_CAP"]=20,["PHIEU_NHAP"]=20,["PHIEU_NHAP_CT"]=40,["NHAT_KY_KHO"]=113,["__PhoneStoreMigrations"]=1
    };
    private sealed class DemoClock(DateTimeOffset current):TimeProvider
    {
        public DateTimeOffset Current{get;set;}=current;
        public override DateTimeOffset GetUtcNow()=>Current;
    }
}
