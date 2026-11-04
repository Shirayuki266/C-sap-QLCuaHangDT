using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PhoneStore.Core.Accounts;
using PhoneStore.Core.Commerce;
using PhoneStore.Infrastructure.Commerce;
using PhoneStore.Infrastructure.Data;

namespace PhoneStore.Checks;

internal sealed record MvcCatalogFixture(long MainId, long OtherId, long SplitId, long OutOfStockId,
    long HiddenProductId, long HiddenBrandId, long HiddenCategoryId, long HiddenVariantsId,
    long Black128, long Blue256, long Black256, long OtherVariant, int BrandId, Actor Admin, Actor Customer)
{
    internal const string Password = "CatalogFixtureQa@12345";
    public static async Task<MvcCatalogFixture> Seed(IServiceProvider services)
    {
        var db = services.GetRequiredService<StoreDbContext>();
        var admin = new Account { Username = "catalog-qa-admin", Role = "ADMIN", Active = true };
        admin.PasswordHash = services.GetRequiredService<IPasswordHasher<Account>>().HashPassword(admin, Password);
        db.Accounts.Add(admin); await db.SaveChangesAsync();
        await services.GetRequiredService<IAccountRegistration>().RegisterAsync(new("catalog-qa-customer", Password, "Khách fixture QA", "0938123501"));
        var sessions = services.GetRequiredService<IAccountSessions>();
        var a = await sessions.LoginAsync(new(admin.Username, Password, "admin")); var c = await sessions.LoginAsync(new("catalog-qa-customer", Password));
        var actor = new Actor(a.Account.Id, a.SessionId); var customer = new Actor(c.Account.Id, c.SessionId);
        var lookups = services.GetRequiredService<LookupService>();
        var brand = await lookups.Save(actor, "brands", 0, new("Hãng QA Alpha"));
        var otherBrand = await lookups.Save(actor, "brands", 0, new("Hãng QA Beta"));
        var inactiveBrand = await lookups.Save(actor, "brands", 0, new("Hãng QA ngừng"));
        var category = await lookups.Save(actor, "categories", 0, new("Điện thoại QA"));
        var inactiveCategory = await lookups.Save(actor, "categories", 0, new("Danh mục QA ngừng"));
        var products = services.GetRequiredService<ProductService>();
        Task<long> Create(string code, string name, List<VariantInput> variants, int? b = null, int? cat = null, string image = "/images/mvc-catalog/phone-a.svg") =>
            products.Save(actor, 0, new(code, name, b ?? brand, cat ?? category, "Màn hình · Camera · Chip xử lý QA", "Sản phẩm kiểm thử, không phải thông tin bán hàng thực tế.", image, true, variants,
                [new("/images/mvc-catalog/phone-b.svg", 0)]));
        var main = await Create("CAT-MAIN", "QA Aurora One", [new(0, "CAT-128-BLACK", "Đen", 128, 10000000m, 20), new(0, "CAT-256-BLUE", "Xanh", 256, 15000000m, 20), new(0, "CAT-256-BLACK", "Đen", 256, 20000000m, 20)]);
        var other = await Create("CAT-OTHER", "QA Aurora Two", [new(0, "CAT-OTHER-256", "Đen", 256, 20000000m, 10)]);
        var split = await Create("CAT-SPLIT", "QA Aurora Split", [new(0, "CAT-SPLIT-128", "Đen", 128, 5000000m, 10), new(0, "CAT-SPLIT-256", "Xanh", 256, 20000000m, 10)]);
        var empty = await Create("CAT-EMPTY", "QA Nova hết hàng", [new(0, "CAT-EMPTY-128", "Đen", 128, 8000000m, 20)], b:otherBrand, image:"/images/mvc-catalog/missing.svg");
        var hidden = await Create("CAT-HIDDEN", "QA Hidden product", [new(0, "CAT-HIDDEN-128", "Đen", 128, 1000m, 20)]);
        var hiddenBrand = await Create("CAT-HIDDEN-BRAND", "QA Hidden brand product", [new(0, "CAT-HIDDEN-BRAND-128", "Đen", 128, 1000m, 20)], b:inactiveBrand);
        var hiddenCategory = await Create("CAT-HIDDEN-CATEGORY", "QA Hidden category product", [new(0, "CAT-HIDDEN-CATEGORY-128", "Đen", 128, 1000m, 20)], cat:inactiveCategory);
        var hiddenVariants = await Create("CAT-HIDDEN-VARIANTS", "QA Hidden variant product", [new(0, "CAT-HIDDEN-VARIANT-128", "Đen", 128, 1000m, 20)]);
        var mainProduct = await products.Read(actor, main); var otherProduct = await products.Read(actor, other); var splitProduct = await products.Read(actor, split);
        var supplier = await services.GetRequiredService<SupplierService>().Save(actor, 0, new("CAT-SUP", "NCC fixture QA", "0938123599", "Địa chỉ QA"));
        var purchases = services.GetRequiredService<PurchaseService>();
        var stockLines = mainProduct.Variants.Select(v => new PurchaseLineInput(v.Id, 8, v.Cost))
            .Concat(otherProduct.Variants.Select(v => new PurchaseLineInput(v.Id, 8, v.Cost)))
            .Concat(splitProduct.Variants.Select(v => new PurchaseLineInput(v.Id, 8, v.Cost))).ToList();
        var receipt = await purchases.Save(actor, 0, new(supplier, stockLines, RequestKey: Guid.NewGuid()));
        await purchases.Confirm(actor, receipt, new((await purchases.Read(actor, receipt)).Version, Guid.NewGuid()));
        var carts = services.GetRequiredService<CartService>(); var checkout = services.GetRequiredService<CheckoutService>();
        var workflow = services.GetRequiredService<OrderWorkflow>(); var orders = services.GetRequiredService<OrderQueryService>();
        async Task Place(bool complete, params CartInput[] lines)
        {
            foreach (var line in lines) await carts.Set(customer, line);
            var cart = await carts.Read(customer);
            var order = await checkout.Place(customer, new(Guid.NewGuid(), "Người nhận QA", "0938123501", "Địa chỉ QA",
                cart.Lines.Select(line => new PriceConfirmation(line.VariantId, line.Quantity, line.Price!.Value)).ToList()));
            if (!complete) return;
            await workflow.Confirm(actor, order.Id, new(order.Version, Guid.NewGuid())); order = await orders.Read(actor, order.Id, true);
            await workflow.Dispatch(actor, order.Id, new(order.Version, Guid.NewGuid())); order = await orders.Read(actor, order.Id, true);
            await workflow.Complete(actor, order.Id, new(order.Version, Guid.NewGuid()));
        }
        await Place(true, new CartInput(mainProduct.Variants[2].Id, 1));
        await Place(true, new CartInput(otherProduct.Variants[0].Id, 2));
        // OnHand remains positive while availability is zero for Blue256; carts themselves reserve nothing.
        await Place(false, new CartInput(mainProduct.Variants[0].Id, 2), new CartInput(mainProduct.Variants[1].Id, 8));
        var current = await products.Read(actor, hidden); await products.Save(actor, hidden, ProductChecks.Input(current) with { Active = false });
        current = await products.Read(actor, hiddenVariants); var input = ProductChecks.Input(current);
        await products.Save(actor, hiddenVariants, input with { Variants = input.Variants!.Select(v => v with { Active = false }).ToList() });
        await lookups.Save(actor, "brands", inactiveBrand, new("Hãng QA ngừng", false));
        await lookups.Save(actor, "categories", inactiveCategory, new("Danh mục QA ngừng", false));
        return new(main, other, split, empty, hidden, hiddenBrand, hiddenCategory, hiddenVariants,
            mainProduct.Variants[0].Id, mainProduct.Variants[1].Id, mainProduct.Variants[2].Id, otherProduct.Variants[0].Id, brand, actor, customer);
    }
}
