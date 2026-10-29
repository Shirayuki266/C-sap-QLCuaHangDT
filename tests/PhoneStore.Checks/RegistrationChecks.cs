using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PhoneStore.Core;
using PhoneStore.Core.Accounts;
using PhoneStore.Infrastructure.Accounts;
using PhoneStore.Infrastructure.Data;

namespace PhoneStore.Checks;

internal static class RegistrationChecks
{
    public static async Task Run(string connection)
    {
        StoreDbContext Open() => new(new DbContextOptionsBuilder<StoreDbContext>().UseSqlServer(connection).Options);
        var hasher = new PasswordHasher<Account>();
        async Task<RegistrationResult> Register(RegistrationRequest input)
        {
            await using var db = Open();
            return await new AccountRegistration(db, hasher).RegisterAsync(input);
        }
        async Task Reject(RegistrationRequest input)
        {
            try { await Register(input); }
            catch (BusinessException exception) when (exception.Code == "INVALID_INPUT") { return; }
            throw new InvalidOperationException("Expected registration validation failure.");
        }
        var valid = new RegistrationRequest("reg-first", "Registration@12345", "Khách đăng ký", "0912345600", "register@example.test", "123 địa chỉ");
        await Check.Run("K01 normalizes account/profile and creates CUSTOMER without a session", async () =>
        {
            await using var before = Open();
            var sessions = await before.Sessions.CountAsync();
            var result = await Register(valid with { Username = "  REG-FIRST  ", Email = " REGISTER@EXAMPLE.TEST ", FullName = " Khách đăng ký ", Phone = " 0912345600 " });
            await using var verify = Open();
            var account = await verify.Accounts.SingleAsync(x => x.Id == result.Id);
            var customer = await verify.Customers.SingleAsync(x => x.Id == result.Id);
            Check.True(account.Username == "reg-first" && account.Role == "CUSTOMER" && account.Active && result.Role == "CUSTOMER", "Registration role/state was not server-derived.");
            Check.True(customer.FullName == "Khách đăng ký" && customer.Phone == valid.Phone && customer.Email == valid.Email, "Profile normalization failed.");
            Check.True(await verify.Sessions.CountAsync() == sessions, "Registration logged the customer in.");
            Check.True(account.PasswordHash != valid.Password && hasher.VerifyHashedPassword(account, account.PasswordHash, valid.Password!) != PasswordVerificationResult.Failed, "Password was not properly hashed.");
        });
        await Check.Run("K01 duplicate username/phone/email rolls back both account and profile", async () =>
        {
            await using var before = Open();
            var accounts = await before.Accounts.CountAsync(); var profiles = await before.Customers.CountAsync();
            await Reject(valid with { Username = "REG-FIRST", Phone = "0912345601", Email = "different@example.test" });
            await Reject(valid with { Username = "reg-duplicate-phone", Email = null });
            await Reject(valid with { Username = "reg-duplicate-email", Phone = "0912345602", Email = "REGISTER@EXAMPLE.TEST" });
            await using var after = Open();
            Check.True(await after.Accounts.CountAsync() == accounts && await after.Customers.CountAsync() == profiles, "Failed registration left an orphan account/profile.");
        });
        await Check.Run("K01 malformed and out-of-capacity input writes no account", async () =>
        {
            await using var before = Open(); var count = await before.Accounts.CountAsync();
            foreach (var invalid in new[] {
                valid with { Username = null }, valid with { Username = "ab" }, valid with { Username = "has space" }, valid with { Username = new string('a',101) },
                valid with { Password = null }, valid with { Password = "short1" }, valid with { Password = new string('a',129) + "1" },
                valid with { Password = "abcdefghijk" }, valid with { Password = "12345678901" }, valid with { FullName = " " },
                valid with { FullName = new string('a',121) }, valid with { Phone = "bad-phone" }, valid with { Phone = "12345678" },
                valid with { Email = "not-email" }, valid with { Email = "Name <valid@example.test>" }, valid with { Email = new string('a',255) },
                valid with { Address = new string('a',501) } })
                await Reject(invalid);
            await using var after = Open();
            Check.True(await after.Accounts.CountAsync() == count, "Invalid registration wrote an account.");
        });
        await Check.Run("K01 optional email is nullable and identical passwords get independent salts", async () =>
        {
            var first = await Register(valid with { Username = "reg-no-email-a", Phone = "0912345603", Email = null });
            var second = await Register(valid with { Username = "reg-no-email-b", Phone = "0912345604", Email = " " });
            await using var db = Open();
            var accountA = await db.Accounts.SingleAsync(x => x.Id == first.Id);
            var accountB = await db.Accounts.SingleAsync(x => x.Id == second.Id);
            Check.True(accountA.PasswordHash != accountB.PasswordHash, "Passwords did not receive independent salts.");
            Check.True(await db.Customers.CountAsync(x => (x.Id == first.Id || x.Id == second.Id) && x.Email == null) == 2, "Optional email was not normalized to null.");
        });
        await Check.Run("K01 independent SQL connections race safely on duplicate registration", async () =>
        {
            async Task<bool> Attempt(RegistrationRequest input)
            {
                try { await Register(input); return true; }
                catch (BusinessException exception) when (exception.Code == "INVALID_INPUT") { return false; }
            }
            var sameUsername = await Task.WhenAll(
                Attempt(valid with { Username = "reg-race-name", Phone = "0912345605", Email = null }),
                Attempt(valid with { Username = "REG-RACE-NAME", Phone = "0912345606", Email = null }));
            var samePhone = await Task.WhenAll(
                Attempt(valid with { Username = "reg-race-phone-a", Phone = "0912345607", Email = null }),
                Attempt(valid with { Username = "reg-race-phone-b", Phone = "0912345607", Email = null }));
            Check.True(sameUsername.Count(success => success) == 1 && samePhone.Count(success => success) == 1, "Concurrent duplicates were accepted.");
            await using var db = Open();
            Check.True(await db.Accounts.CountAsync(x => x.Username.StartsWith("reg-race-")) == 2 &&
                await db.Customers.CountAsync(x => x.Phone == "0912345607") == 1, "A losing concurrent request left partial data.");
        });
    }
}
