using PhoneStore.Core.Commerce;

namespace PhoneStore.Core;

/// <summary>In-process customer MVC boundary. Existing services remain authoritative.</summary>
public interface IStoreService
{
    Task<CartView> ReadCartAsync(Actor actor);
    Task AddToCartAsync(Actor actor, long productId, long variantId, int quantity);
}
