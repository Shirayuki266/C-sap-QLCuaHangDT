using Microsoft.EntityFrameworkCore;

namespace PhoneStore.Infrastructure.Data;

public sealed class StoreDbContext(DbContextOptions<StoreDbContext> options) : DbContext(options)
{
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<LoginSession> Sessions => Set<LoginSession>();
    public DbSet<Brand> Brands => Set<Brand>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    public DbSet<Variant> Variants => Set<Variant>();
    public DbSet<Inventory> Inventory => Set<Inventory>();
    public DbSet<CartLine> Cart => Set<CartLine>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderLine> OrderLines => Set<OrderLine>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<OrderHistory> OrderHistory => Set<OrderHistory>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<Purchase> Purchases => Set<Purchase>();
    public DbSet<PurchaseLine> PurchaseLines => Set<PurchaseLine>();
    public DbSet<StockMovement> Movements => Set<StockMovement>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<Account>(e =>
        {
            e.ToTable("TAI_KHOAN"); e.HasKey(x => x.Id);
            e.Property(x => x.Username).HasMaxLength(100); e.HasIndex(x => x.Username).IsUnique();
            e.Property(x => x.PasswordHash).HasMaxLength(1000); e.Property(x => x.Role).HasMaxLength(20).IsUnicode(false);
        });
        model.Entity<Customer>(e =>
        {
            e.ToTable("KHACH_HANG"); e.HasKey(x => x.Id); e.Property(x => x.Id).ValueGeneratedNever();
            e.HasOne<Account>().WithOne().HasForeignKey<Customer>(x => x.Id);
            e.Property(x => x.FullName).HasMaxLength(120); e.Property(x => x.Phone).HasMaxLength(20).IsUnicode(false);
            e.Property(x => x.Email).HasMaxLength(254); e.Property(x => x.Address).HasMaxLength(500);
            e.HasIndex(x => x.Phone).IsUnique(); e.HasIndex(x => x.Email).IsUnique().HasFilter("[Email] IS NOT NULL");
        });
        model.Entity<LoginSession>(e =>
        {
            e.ToTable("PHIEN"); e.HasKey(x => x.Id); e.Property(x => x.Id).ValueGeneratedNever();
            e.HasOne<Account>().WithMany().HasForeignKey(x => x.AccountId); e.HasIndex(x => x.ExpiresAt);
        });
        model.Entity<Brand>(e => { e.ToTable("HANG"); e.HasKey(x => x.Id); e.Property(x => x.Name).HasMaxLength(100); e.HasIndex(x => x.Name).IsUnique(); });
        model.Entity<Category>(e => { e.ToTable("DANH_MUC"); e.HasKey(x => x.Id); e.Property(x => x.Name).HasMaxLength(100); e.HasIndex(x => x.Name).IsUnique(); });
        model.Entity<Product>(e =>
        {
            e.ToTable("SAN_PHAM"); e.HasKey(x => x.Id); e.Property(x => x.Code).HasMaxLength(30); e.HasIndex(x => x.Code).IsUnique();
            e.Property(x => x.Name).HasMaxLength(200); e.Property(x => x.ImageUrl).HasMaxLength(1000);
            e.Property(x => x.Specifications).HasMaxLength(20000); e.Property(x => x.Description).HasMaxLength(50000);
            e.HasOne<Brand>().WithMany().HasForeignKey(x => x.BrandId); e.HasOne<Category>().WithMany().HasForeignKey(x => x.CategoryId);
        });
        model.Entity<ProductImage>(e =>
        {
            e.ToTable("SAN_PHAM_ANH"); e.HasKey(x => x.Id); e.HasOne<Product>().WithMany().HasForeignKey(x => x.ProductId);
            e.Property(x => x.Url).HasMaxLength(1000);
            e.Property<byte[]>("UrlHash").HasComputedColumnSql("CONVERT(binary(32),HASHBYTES('SHA2_256',[Url]))", stored: true);
            e.HasIndex(nameof(ProductImage.ProductId), "UrlHash").IsUnique();
        });
        model.Entity<Variant>(e =>
        {
            e.ToTable("BIEN_THE"); e.HasKey(x => x.Id); e.HasOne<Product>().WithMany().HasForeignKey(x => x.ProductId);
            e.Property(x => x.Sku).HasMaxLength(50); e.HasIndex(x => x.Sku).IsUnique(); e.Property(x => x.Color).HasMaxLength(50);
            e.HasIndex(x => new { x.ProductId, x.Color, x.StorageGb }).IsUnique();
            e.Property(x => x.Cost).HasPrecision(18, 2); e.Property(x => x.Price).HasPrecision(18, 2); e.Property(x => x.ProfitPercent).HasPrecision(7, 2);
            e.Property(x => x.Version).IsRowVersion();
        });
        model.Entity<Inventory>(e =>
        {
            e.ToTable("TON_KHO"); e.HasKey(x => x.VariantId); e.Property(x => x.VariantId).ValueGeneratedNever();
            e.HasOne<Variant>().WithOne().HasForeignKey<Inventory>(x => x.VariantId); e.Property(x => x.Version).IsRowVersion();
        });
        model.Entity<CartLine>(e =>
        {
            e.ToTable("GIO_HANG_CT"); e.HasKey(x => new { x.CustomerId, x.VariantId });
            e.HasOne<Customer>().WithMany().HasForeignKey(x => x.CustomerId); e.HasOne<Variant>().WithMany().HasForeignKey(x => x.VariantId);
        });
        model.Entity<Order>(e =>
        {
            e.ToTable("DON_HANG"); e.HasKey(x => x.Id); e.Property(x => x.Code).HasMaxLength(40).IsUnicode(false); e.HasIndex(x => x.Code).IsUnique();
            e.HasOne<Customer>().WithMany().HasForeignKey(x => x.CustomerId); e.Property(x => x.Recipient).HasMaxLength(120);
            e.Property(x => x.Phone).HasMaxLength(20).IsUnicode(false); e.Property(x => x.Address).HasMaxLength(500);
            e.Property(x => x.Status).HasMaxLength(30).IsUnicode(false); e.Property(x => x.Total).HasPrecision(18, 2);
            e.HasIndex(x => x.RequestKey).IsUnique(); e.Property(x => x.Version).IsRowVersion();
            e.HasIndex(x => new { x.CustomerId, x.CreatedAt }); e.HasIndex(x => new { x.Status, x.CompletedAt });
        });
        model.Entity<OrderLine>(e =>
        {
            e.ToTable("DON_HANG_CT"); e.HasKey(x => x.Id); e.HasOne<Order>().WithMany().HasForeignKey(x => x.OrderId);
            e.HasOne<Variant>().WithMany().HasForeignKey(x => x.VariantId); e.Property(x => x.Name).HasMaxLength(200);
            e.Property(x => x.Sku).HasMaxLength(50); e.Property(x => x.Description).HasMaxLength(500);
            e.Property(x => x.Price).HasPrecision(18, 2); e.HasIndex(x => new { x.OrderId, x.VariantId }).IsUnique();
        });
        model.Entity<Payment>(e =>
        {
            e.ToTable("THANH_TOAN"); e.HasKey(x => x.OrderId); e.Property(x => x.OrderId).ValueGeneratedNever();
            e.HasOne<Order>().WithOne().HasForeignKey<Payment>(x => x.OrderId); e.HasOne<Account>().WithMany().HasForeignKey(x => x.ActorId);
            e.Property(x => x.Method).HasMaxLength(10).IsUnicode(false); e.Property(x => x.Status).HasMaxLength(20).IsUnicode(false); e.Property(x => x.Amount).HasPrecision(18, 2);
        });
        model.Entity<OrderHistory>(e =>
        {
            e.ToTable("LICH_SU_DON"); e.HasKey(x => x.Id); e.HasOne<Order>().WithMany().HasForeignKey(x => x.OrderId);
            e.HasOne<Account>().WithMany().HasForeignKey(x => x.ActorId); e.Property(x => x.From).HasMaxLength(30).IsUnicode(false);
            e.Property(x => x.To).HasMaxLength(30).IsUnicode(false); e.Property(x => x.Reason).HasMaxLength(1000);
            e.HasIndex(x => new { x.OrderId, x.Id });
        });
        model.Entity<Supplier>(e =>
        {
            e.ToTable("NHA_CUNG_CAP"); e.HasKey(x => x.Id); e.Property(x => x.Code).HasMaxLength(30); e.HasIndex(x => x.Code).IsUnique();
            e.Property(x => x.Name).HasMaxLength(200); e.Property(x => x.Phone).HasMaxLength(20).IsUnicode(false); e.Property(x => x.Address).HasMaxLength(500);
        });
        model.Entity<Purchase>(e =>
        {
            e.ToTable("PHIEU_NHAP"); e.HasKey(x => x.Id); e.Property(x => x.Code).HasMaxLength(40).IsUnicode(false); e.HasIndex(x => x.Code).IsUnique();
            e.HasOne<Supplier>().WithMany().HasForeignKey(x => x.SupplierId); e.HasOne<Account>().WithMany().HasForeignKey(x => x.CreatorId);
            e.HasOne<Account>().WithMany().HasForeignKey(x => x.ConfirmerId); e.Property(x => x.Status).HasMaxLength(30).IsUnicode(false);
            e.Property(x => x.Total).HasPrecision(18, 2); e.Property(x => x.Version).IsRowVersion();
        });
        model.Entity<PurchaseLine>(e =>
        {
            e.ToTable("PHIEU_NHAP_CT"); e.HasKey(x => x.Id); e.HasOne<Purchase>().WithMany().HasForeignKey(x => x.PurchaseId);
            e.HasOne<Variant>().WithMany().HasForeignKey(x => x.VariantId); e.Property(x => x.Cost).HasPrecision(18, 2);
            e.HasIndex(x => new { x.PurchaseId, x.VariantId }).IsUnique();
        });
        model.Entity<StockMovement>(e =>
        {
            e.ToTable("NHAT_KY_KHO"); e.HasKey(x => x.Id); e.HasOne<Variant>().WithMany().HasForeignKey(x => x.VariantId);
            e.HasOne<Account>().WithMany().HasForeignKey(x => x.ActorId); e.HasOne<Order>().WithMany().HasForeignKey(x => x.OrderId);
            e.HasOne<Purchase>().WithMany().HasForeignKey(x => x.PurchaseId); e.Property(x => x.Kind).HasMaxLength(30).IsUnicode(false);
            e.Property(x => x.Reason).HasMaxLength(1000); e.HasIndex(x => new { x.OperationKey, x.VariantId }).IsUnique();
            e.HasIndex(x => new { x.OrderId, x.VariantId, x.Kind }).IsUnique().HasFilter("[OrderId] IS NOT NULL");
            e.HasIndex(x => new { x.PurchaseId, x.VariantId, x.Kind }).IsUnique().HasFilter("[PurchaseId] IS NOT NULL");
            e.HasIndex(x => new { x.VariantId, x.Id });
        });
        foreach (var relationship in model.Model.GetEntityTypes().SelectMany(entity => entity.GetForeignKeys()))
            relationship.DeleteBehavior = DeleteBehavior.NoAction;
    }
}
