using PhoneStore.Core;
using PhoneStore.Core.Commerce;
using PhoneStore.Core.Accounts;
using PhoneStore.Infrastructure.Commerce;

namespace PhoneStore.Web.Storefront;

public sealed class MvcStoreAdapter(CatalogService catalog, CartService carts,
    IAccountRegistration registration, IAccountSessions sessions, IAccountProfiles profiles,
    IAccountPasswords passwords, LookupService lookups, PublicBestsellerService bestsellers) : IStoreService
{
    public Task<Page<PublicProduct>> ReadCatalogAsync(CatalogFilter filter) => catalog.List(filter);
    public async Task<PublicProduct> ReadProductAsync(long productId, long? variantId = null)
    {
        var product = await catalog.Read(productId);
        if (variantId.HasValue) RequireSelection(product, variantId.Value);
        return product;
    }
    public async Task<List<CatalogBrand>> ReadBrandsAsync() =>
        (await lookups.Public("brands")).Select(brand => new CatalogBrand(brand.Id, brand.Name)).ToList();
    public Task<PublicBestsellers> ReadBestsellersAsync() => bestsellers.Read();
    private static void RequireSelection(PublicProduct product, long variantId)
    {
        if (!product.Variants.Any(variant => variant.Id == variantId))
            throw new BusinessException("NOT_FOUND", "Không tìm thấy lựa chọn sản phẩm.");
    }
    public async Task RegisterAsync(RegistrationRequest request, CancellationToken cancellationToken = default) =>
        await registration.RegisterAsync(request, cancellationToken);
    public Task<AccountSession> LoginAsync(string? username, string? password, CancellationToken cancellationToken = default) =>
        sessions.LoginAsync(new(username, password, "customer"), cancellationToken);
    public Task LogoutAsync(Actor actor, CancellationToken cancellationToken = default) =>
        sessions.RevokeAsync(actor.AccountId, actor.SessionId, cancellationToken);
    public Task<CustomerProfile> ReadProfileAsync(Actor actor, CancellationToken cancellationToken = default) =>
        profiles.ReadAsync(actor.AccountId, actor.SessionId, cancellationToken);
    public Task UpdateProfileAsync(Actor actor, ProfileChangeRequest request, CancellationToken cancellationToken = default) =>
        profiles.UpdateAsync(actor.AccountId, actor.SessionId, request, cancellationToken);
    public Task ChangePasswordAsync(Actor actor, PasswordChangeRequest request, CancellationToken cancellationToken = default) =>
        passwords.ChangeAsync(actor.AccountId, actor.SessionId, request, cancellationToken);

    public Task<CartView> ReadCartAsync(Actor actor) => carts.Read(actor);
    public Task<CartView> QuoteCartAsync(List<CartInput> lines) => carts.Preview(lines);
    public Task<CartMergeResult> MergeCartAsync(Actor actor, List<CartInput> lines) => carts.Merge(actor, lines);
    public Task UpdateCartAsync(Actor actor, long variantId, int quantity) => carts.Set(actor, new(variantId, quantity));
    public Task RemoveCartAsync(Actor actor, long variantId) => carts.Set(actor, new(variantId, 0));

    public async Task AddToCartAsync(Actor actor, long productId, long variantId, int quantity)
    {
        // Approved MVC input association check, read only. Never price, reserve or authorize here.
        await ReadProductAsync(productId, variantId);
        await carts.Set(actor, new(variantId, quantity), add: true);
    }
}
