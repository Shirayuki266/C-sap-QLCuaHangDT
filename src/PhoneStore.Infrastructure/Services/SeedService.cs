using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PhoneStore.Infrastructure.Data;
namespace PhoneStore.Infrastructure.Services;
public sealed partial class StoreService
{
    public Task<bool> Seed(string adminPassword,string customerPassword)=>Atomic(async()=> {
        if(await db.Accounts.AnyAsync()) return false;
        Password(adminPassword); Password(customerPassword);
        var admin=new Account{Username="admin",Role="ADMIN"}; admin.PasswordHash=new PasswordHasher<Account>().HashPassword(admin,adminPassword);
        var account=new Account{Username="khachhang"}; account.PasswordHash=new PasswordHasher<Account>().HashPassword(account,customerPassword);
        db.Accounts.Add(admin); db.Customers.Add(new Customer{Account=account,FullName="Nguyễn Minh An",Phone="0901234567",Email="an@example.test",Address="12 Nguyễn Trãi, Quận 1, TP. Hồ Chí Minh"});
        var apple=new Brand{Name="Apple"}; var samsung=new Brand{Name="Samsung"}; var xiaomi=new Brand{Name="Xiaomi"}; var category=new Category{Name="Điện thoại thông minh"};
        var products=new[]{
            new Product{Code="IP16",Name="iPhone 16",Brand=apple,Category=category,Specifications="OLED 6.1 inch · Apple A18 · Camera 48 MP",Description="Điện thoại Apple với nhiều lựa chọn màu và dung lượng.",ImageUrl="/images/phone.svg",Variants=[new Variant{Sku="IP16-128-BLK",Color="Đen",StorageGb=128,Price=19990000m,Inventory=new Inventory()},new Variant{Sku="IP16-256-BLU",Color="Xanh",StorageGb=256,Price=22990000m,Inventory=new Inventory()}]},
            new Product{Code="S25",Name="Galaxy S25",Brand=samsung,Category=category,Specifications="AMOLED 6.2 inch · Snapdragon 8 Elite · RAM 12 GB",Description="Màn hình 120 Hz và camera đa tiêu cự.",ImageUrl="/images/phone.svg",Variants=[new Variant{Sku="S25-256-SLV",Color="Bạc",StorageGb=256,Price=20990000m,Inventory=new Inventory()}]},
            new Product{Code="XM14",Name="Xiaomi 14",Brand=xiaomi,Category=category,Specifications="AMOLED 6.36 inch · Snapdragon 8 Gen 3 · Camera Leica",Description="Thiết kế nhỏ gọn, sạc nhanh.",ImageUrl="/images/phone.svg",Variants=[new Variant{Sku="XM14-256-GRN",Color="Xanh",StorageGb=256,Price=14990000m,Inventory=new Inventory()}]}
        };
        db.Products.AddRange(products); var supplier=new Supplier{Code="NCC001",Name="Nhà phân phối Điện thoại Việt",Phone="0281234567",Address="TP. Hồ Chí Minh"}; db.Suppliers.Add(supplier); await db.SaveChangesAsync();
        var receipt=new Purchase{Code="PN-KHOI-TAO",Supplier=supplier,Creator=admin,Confirmer=admin,Status="DA_XAC_NHAN",ConfirmedAt=DateTime.UtcNow}; var op=Guid.NewGuid();
        foreach(var v in products.SelectMany(x=>x.Variants)) { var quantity=v.Sku.StartsWith("XM")?0:10; if(quantity==0) continue; var cost=decimal.Round(v.Price*0.8m,0); receipt.Lines.Add(new PurchaseLine{VariantId=v.Id,Quantity=quantity,Cost=cost}); v.Inventory.OnHand=quantity; }
        receipt.Total=receipt.Lines.Sum(x=>x.Quantity*x.Cost); db.Purchases.Add(receipt); await db.SaveChangesAsync();
        foreach(var line in receipt.Lines) db.Movements.Add(new Movement{VariantId=line.VariantId,ActorId=admin.Id,PurchaseId=receipt.Id,Kind="NHAP",DeltaOnHand=line.Quantity,OperationKey=op,Reason="Phiếu nhập dữ liệu mẫu"}); return true;
    });
}
