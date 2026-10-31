using PhoneStore.Core.Commerce;
using PhoneStore.Core.Accounts;

namespace PhoneStore.Core;

/// <summary>In-process customer MVC boundary. Existing services remain authoritative.</summary>
public interface IStoreService
{
    Task<Page<PublicProduct>> ReadCatalogAsync(CatalogFilter filter);
    Task<PublicProduct> ReadProductAsync(long productId, long? variantId = null);
    Task<List<CatalogBrand>> ReadBrandsAsync();
    Task<PublicBestsellers> ReadBestsellersAsync();
    Task RegisterAsync(RegistrationRequest request, CancellationToken cancellationToken = default);
    Task<AccountSession> LoginAsync(string? username, string? password, CancellationToken cancellationToken = default);
    Task LogoutAsync(Actor actor, CancellationToken cancellationToken = default);
    Task<CustomerProfile> ReadProfileAsync(Actor actor, CancellationToken cancellationToken = default);
    Task UpdateProfileAsync(Actor actor, ProfileChangeRequest request, CancellationToken cancellationToken = default);
    Task ChangePasswordAsync(Actor actor, PasswordChangeRequest request, CancellationToken cancellationToken = default);
    Task<CartView> ReadCartAsync(Actor actor);
    Task AddToCartAsync(Actor actor, long productId, long variantId, int quantity);
    Task<CartView> QuoteCartAsync(List<CartInput> lines);
    Task<CartMergeResult> MergeCartAsync(Actor actor, List<CartInput> lines);
    Task UpdateCartAsync(Actor actor, long variantId, int quantity);
    Task RemoveCartAsync(Actor actor, long variantId);
}

public sealed record CatalogBrand(int Id, string Name);
