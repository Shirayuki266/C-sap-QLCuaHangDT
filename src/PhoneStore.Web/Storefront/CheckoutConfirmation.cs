using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using PhoneStore.Core.Commerce;

namespace PhoneStore.Web.Storefront;

// Protect the displayed server quote; posted price/total/line fields are never used as authority.
public sealed class CheckoutConfirmation(IDataProtectionProvider provider)
{
    private readonly IDataProtector protector = provider.CreateProtector("PhoneStore.Mvc.CheckoutConfirmation.v1");
    private sealed record Quote(long Account, Guid RequestKey, List<PriceConfirmation> Lines);
    public string Protect(long account, Guid key, CartView cart) => protector.Protect(JsonSerializer.Serialize(
        new Quote(account, key, cart.Lines.Select(line => new PriceConfirmation(line.VariantId, line.Quantity, line.Price ?? 0)).ToList())));
    public List<PriceConfirmation>? Read(string? token, long account, Guid key)
    {
        if (string.IsNullOrEmpty(token) || key == Guid.Empty) return null;
        try
        {
            var quote = JsonSerializer.Deserialize<Quote>(protector.Unprotect(token));
            return quote?.Account == account && quote.RequestKey == key ? quote.Lines : null;
        }
        catch (Exception error) when (error is CryptographicException or JsonException) { return null; }
    }
}
