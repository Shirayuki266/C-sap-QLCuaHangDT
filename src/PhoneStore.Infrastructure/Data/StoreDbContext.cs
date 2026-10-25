using Microsoft.EntityFrameworkCore;
namespace PhoneStore.Infrastructure.Data;
public sealed class StoreDbContext(DbContextOptions<StoreDbContext> options) : DbContext(options)
{
    public DbSet<Account> Accounts => Set<Account>(); public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<LoginSession> Sessions => Set<LoginSession>(); public DbSet<Brand> Brands => Set<Brand>();
    public DbSet<Category> Categories => Set<Category>(); public DbSet<Product> Products => Set<Product>();
    public DbSet<Variant> Variants => Set<Variant>(); public DbSet<Inventory> Stock => Set<Inventory>();
    public DbSet<CartLine> Cart => Set<CartLine>(); public DbSet<SalesOrder> Orders => Set<SalesOrder>();
    public DbSet<SalesLine> OrderLines => Set<SalesLine>(); public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<OrderHistory> Histories => Set<OrderHistory>(); public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<Purchase> Purchases => Set<Purchase>(); public DbSet<PurchaseLine> PurchaseLines => Set<PurchaseLine>();
    public DbSet<Movement> Movements => Set<Movement>();
    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<ProductImage>().ToTable("SAN_PHAM_ANH",t=>t.HasCheckConstraint("CK_ProductImages_Order","[SortOrder]>=0"));b.Entity<ProductImage>().Property(x=>x.Url).HasMaxLength(1000);b.Entity<ProductImage>().HasIndex(x=>new{x.ProductId,x.SortOrder});b.Entity<ProductImage>().HasOne(x=>x.Product).WithMany(x=>x.Images).HasForeignKey(x=>x.ProductId).OnDelete(DeleteBehavior.NoAction);
        b.Entity<Account>().ToTable("TAI_KHOAN", t => t.HasCheckConstraint("CK_Account_Role", "[Role] IN ('ADMIN','CUSTOMER')"));
        b.Entity<Account>().HasIndex(x=>x.Username).IsUnique(); b.Entity<Account>().Property(x=>x.Username).HasMaxLength(100);
        b.Entity<Customer>().ToTable("KHACH_HANG"); b.Entity<Customer>().HasOne(x=>x.Account).WithOne().HasForeignKey<Customer>(x=>x.Id);
        b.Entity<Customer>().HasIndex(x=>x.Phone).IsUnique(); b.Entity<Customer>().HasIndex(x=>x.Email).IsUnique().HasFilter("[Email] IS NOT NULL");
        b.Entity<Customer>().Property(x=>x.Phone).HasMaxLength(20); b.Entity<Customer>().Property(x=>x.Email).HasMaxLength(254); b.Entity<Customer>().Property(x=>x.FullName).HasMaxLength(120);
        b.Entity<LoginSession>().ToTable("PHIEN"); b.Entity<LoginSession>().HasIndex(x=>x.ExpiresAt);
        b.Entity<Brand>().ToTable("HANG"); b.Entity<Brand>().HasIndex(x=>x.Name).IsUnique(); b.Entity<Brand>().Property(x=>x.Name).HasMaxLength(100);
        b.Entity<Category>().ToTable("DANH_MUC"); b.Entity<Category>().HasIndex(x=>x.Name).IsUnique(); b.Entity<Category>().Property(x=>x.Name).HasMaxLength(100);
        b.Entity<Product>().ToTable("SAN_PHAM"); b.Entity<Product>().HasIndex(x=>x.Code).IsUnique(); b.Entity<Product>().Property(x=>x.Code).HasMaxLength(30); b.Entity<Product>().Property(x=>x.Name).HasMaxLength(200); b.Entity<Product>().Property(x=>x.ImageUrl).HasMaxLength(1000); b.Entity<Product>().Property(x=>x.Specifications).HasColumnType("nvarchar(max)"); b.Entity<Product>().Property(x=>x.Description).HasColumnType("nvarchar(max)"); b.Entity<Product>().HasIndex(x=>new{x.BrandId,x.CategoryId});
        b.Entity<Variant>().ToTable("BIEN_THE",t=>t.HasCheckConstraint("CK_Variant_Positive","[Price] > 0 AND [StorageGb] > 0")); b.Entity<Variant>().HasIndex(x=>x.Sku).IsUnique(); b.Entity<Variant>().HasIndex(x=>new{x.ProductId,x.Color,x.StorageGb}).IsUnique(); b.Entity<Variant>().Property(x=>x.Sku).HasMaxLength(50); b.Entity<Variant>().Property(x=>x.Color).HasMaxLength(50); b.Entity<Variant>().Property(x=>x.Version).IsRowVersion();
        b.Entity<Inventory>().ToTable("TON_KHO",t=>t.HasCheckConstraint("CK_Stock_Balance","[OnHand] >= 0 AND [Reserved] >= 0 AND [Reserved] <= [OnHand]")); b.Entity<Inventory>().HasKey(x=>x.VariantId); b.Entity<Inventory>().HasOne(x=>x.Variant).WithOne(x=>x.Inventory).HasForeignKey<Inventory>(x=>x.VariantId); b.Entity<Inventory>().Property(x=>x.Version).IsRowVersion();
        b.Entity<CartLine>().ToTable("GIO_HANG_CT",t=>t.HasCheckConstraint("CK_Cart_Quantity","[Quantity] > 0")); b.Entity<CartLine>().HasKey(x=>new{x.CustomerId,x.VariantId});
        b.Entity<SalesOrder>().ToTable("DON_HANG",t=>{t.HasCheckConstraint("CK_Order_Total","[Total] > 0"); t.HasCheckConstraint("CK_Order_Status","[Status] IN ('CHO_XAC_NHAN','DA_XAC_NHAN','DANG_GIAO','HOAN_TAT','DA_HUY','GIAO_THAT_BAI')");}); b.Entity<SalesOrder>().HasIndex(x=>x.RequestKey).IsUnique(); b.Entity<SalesOrder>().HasIndex(x=>x.Code).IsUnique(); b.Entity<SalesOrder>().Property(x=>x.Code).HasMaxLength(30); b.Entity<SalesOrder>().Property(x=>x.Recipient).HasMaxLength(120); b.Entity<SalesOrder>().Property(x=>x.Phone).HasMaxLength(20); b.Entity<SalesOrder>().Property(x=>x.Version).IsRowVersion(); b.Entity<SalesOrder>().HasIndex(x=>new{x.CustomerId,x.CreatedAt}); b.Entity<SalesOrder>().HasIndex(x=>new{x.Status,x.CreatedAt});
        b.Entity<SalesLine>().ToTable("DON_HANG_CT",t=>t.HasCheckConstraint("CK_OrderLine_Positive","[Quantity] > 0 AND [Price] > 0")); b.Entity<SalesLine>().HasIndex(x=>new{x.OrderId,x.VariantId}).IsUnique(); b.Entity<SalesLine>().Property(x=>x.Name).HasMaxLength(200); b.Entity<SalesLine>().Property(x=>x.Sku).HasMaxLength(50); b.Entity<SalesLine>().Property(x=>x.Description).HasMaxLength(100);
        b.Entity<Payment>().ToTable("THANH_TOAN",t=>t.HasCheckConstraint("CK_Payment_COD","[Method] = 'COD' AND (([Status]='CHUA_THU' AND [Amount]=0 AND [CollectedAt] IS NULL AND [ActorId] IS NULL) OR ([Status]='DA_THU' AND [Amount]>0 AND [CollectedAt] IS NOT NULL AND [ActorId] IS NOT NULL))")); b.Entity<Payment>().HasKey(x=>x.OrderId); b.Entity<Payment>().HasOne(x=>x.Order).WithOne(x=>x.Payment).HasForeignKey<Payment>(x=>x.OrderId);
        b.Entity<OrderHistory>().ToTable("LICH_SU_DON"); b.Entity<OrderHistory>().HasIndex(x=>new{x.OrderId,x.At});
        b.Entity<Supplier>().ToTable("NHA_CUNG_CAP"); b.Entity<Supplier>().HasIndex(x=>x.Code).IsUnique(); b.Entity<Supplier>().Property(x=>x.Code).HasMaxLength(30); b.Entity<Supplier>().Property(x=>x.Name).HasMaxLength(200);
        b.Entity<Purchase>().ToTable("PHIEU_NHAP",t=>t.HasCheckConstraint("CK_Purchase_State","[Total]>=0 AND [Status] IN ('NHAP','DA_XAC_NHAN','DA_HUY')")); b.Entity<Purchase>().HasIndex(x=>x.Code).IsUnique(); b.Entity<Purchase>().Property(x=>x.Code).HasMaxLength(30); b.Entity<Purchase>().Property(x=>x.Version).IsRowVersion(); b.Entity<Purchase>().HasOne(x=>x.Creator).WithMany().HasForeignKey(x=>x.CreatorId); b.Entity<Purchase>().HasOne(x=>x.Confirmer).WithMany().HasForeignKey(x=>x.ConfirmerId);
        b.Entity<PurchaseLine>().ToTable("PHIEU_NHAP_CT",t=>t.HasCheckConstraint("CK_PurchaseLine_Positive","[Quantity] > 0 AND [Cost] > 0")); b.Entity<PurchaseLine>().HasIndex(x=>new{x.PurchaseId,x.VariantId}).IsUnique();
        b.Entity<Movement>().ToTable("NHAT_KY_KHO",t=>{t.HasCheckConstraint("CK_Movement_Delta","([DeltaOnHand]<>0 OR [DeltaReserved]<>0) AND NOT ([OrderId] IS NOT NULL AND [PurchaseId] IS NOT NULL)"); t.HasCheckConstraint("CK_Movement_Kind","[Kind] IN ('GIU','HUY_GIU','XUAT_GIAO','NHAN_LAI','NHAP','DIEU_CHINH')");}); b.Entity<Movement>().HasIndex(x=>new{x.OperationKey,x.VariantId}).IsUnique(); b.Entity<Movement>().HasIndex(x=>new{x.OrderId,x.VariantId,x.Kind}).IsUnique().HasFilter("[OrderId] IS NOT NULL"); b.Entity<Movement>().Property(x=>x.Kind).HasMaxLength(20); b.Entity<Movement>().HasIndex(x=>new{x.VariantId,x.At});
        foreach(var entity in b.Model.GetEntityTypes()) {
            foreach(var fk in entity.GetForeignKeys()) fk.DeleteBehavior = DeleteBehavior.NoAction;
            foreach(var property in entity.GetProperties()) {
                if(property.ClrType==typeof(decimal)) { property.SetPrecision(18); property.SetScale(2); }
                if(property.ClrType==typeof(string) && property.GetMaxLength()==null && property.GetColumnType()==null) property.SetMaxLength(500);
            }
        }
    }
}
