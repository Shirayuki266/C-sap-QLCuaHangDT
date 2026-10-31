using PhoneStore.Core;
using PhoneStore.Core.Commerce;
using PhoneStore.Core.Accounts;
using PhoneStore.Infrastructure.Commerce;

namespace PhoneStore.Web.Storefront;

public sealed class MvcStoreAdapter(CatalogService catalog, CartService carts,
    IAccountRegistration registration, IAccountSessions sessions, IAccountProfiles profiles,
    IAccountPasswords passwords) : IStoreService
{
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

    public async Task AddToCartAsync(Actor actor, long productId, long variantId, int quantity)
    {
        // Approved MVC input association check, read only. Never price, reserve or authorize here.
        var product = await catalog.Read(productId);
        if (!product.Variants.Any(variant => variant.Id == variantId))
            throw new BusinessException("NOT_FOUND", "Không tìm thấy lựa chọn sản phẩm.");
        await carts.Set(actor, new(variantId, quantity), add: true);
    }
}
