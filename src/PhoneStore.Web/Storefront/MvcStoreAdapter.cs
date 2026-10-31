using PhoneStore.Core;
using PhoneStore.Core.Commerce;
using PhoneStore.Infrastructure.Commerce;

namespace PhoneStore.Web.Storefront;

public sealed class MvcStoreAdapter(CatalogService catalog, CartService carts) : IStoreService
{
    public Task<CartView> ReadCartAsync(Actor actor) => carts.Read(actor);

    public async Task AddToCartAsync(Actor actor, long productId, long variantId, int quantity)
    {
        // Approved MVC input association check, read only. Never price, reserve or authorize here.
        var product = await catalog.Read(productId);
        if (!product.Variants.Any(variant => variant.Id == variantId))
            throw new BusinessException("NOT_FOUND", "Không tìm thấy lựa chọn sản phẩm.");
        await carts.Set(actor, new(variantId, quantity), add: true);
    }
}
