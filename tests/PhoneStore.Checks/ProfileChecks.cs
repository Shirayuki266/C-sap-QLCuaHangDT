using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PhoneStore.Core;
using PhoneStore.Core.Commerce;
using PhoneStore.Infrastructure.Commerce;
using PhoneStore.Core.Accounts;
using PhoneStore.Infrastructure.Accounts;
using PhoneStore.Infrastructure.Data;

namespace PhoneStore.Checks;

internal static class ProfileChecks
{
    public static async Task Run(Uri address, string connection)
    {
        StoreDbContext Open() => new(new DbContextOptionsBuilder<StoreDbContext>().UseSqlServer(connection).Options);
        const string password = "ProfileChecks@12345";
        long firstId, secondId, snapshotOrderId;
        await using (var db = Open())
        {
            var registrations = new AccountRegistration(db, new PasswordHasher<Account>());
            firstId = (await registrations.RegisterAsync(new("profile-first", password, "First Customer", "0956789001", "first-profile@example.test", "Old profile address"))).Id;
            secondId = (await registrations.RegisterAsync(new("profile-second", password, "Second Customer", "0956789002", "second-profile@example.test", "Other address"))).Id;
            var admin = new Account { Username = "profile-admin", Role = "ADMIN" };
            admin.PasswordHash = new PasswordHasher<Account>().HashPassword(admin, password); db.Add(admin);
            await db.SaveChangesAsync();
        }
        using var first = new ApiTestClient(address); using var second = new ApiTestClient(address); using var adminClient = new ApiTestClient(address); using var guest = new ApiTestClient(address);
        foreach (var tuple in new[] { (first, "profile-first", "customer"), (second, "profile-second", "customer"), (adminClient, "profile-admin", "admin") })
        {
            using var login = await tuple.Item1.Send("/api/auth/login", new LoginRequest(tuple.Item2, password, tuple.Item3));
            Check.True(login.IsSuccessStatusCode, "Profile fixture login failed.");
        }
        using (var fixture = new CommerceContext(connection,address,"profile-history"))
        {
            await fixture.Initialize();var data=await PurchaseConfirmChecks.Receive(fixture,await CommerceData.Create(fixture));
            await using var db=Open();var session=await db.Sessions.AsNoTracking().FirstAsync(x=>x.AccountId==firstId);var actor=new Actor(firstId,session.Id);var k=fixture.Kernel(db);var cart=new CartService(k);
            await cart.Set(actor,new(data.Product.Variants[0].Id,1));var current=await cart.Read(actor);
            snapshotOrderId=(await new CheckoutService(k).Place(actor,new(Guid.NewGuid(),"Snapshot Recipient","0956789001","Snapshot address",current.Lines.Select(x=>new PriceConfirmation(x.VariantId,x.Quantity,x.Price!.Value)).ToList()))).Id;
        }
        var valid = new ProfileChangeRequest("Updated Customer", "0956789003", "new-profile@example.test", "New address");
        async Task<CustomerProfile> Read(ApiTestClient client)
        {
            using var response = await client.Http.GetAsync("/api/account/profile");
            Check.True(response.IsSuccessStatusCode, "Profile query failed.");
            return (await response.Content.ReadFromJsonAsync<CustomerProfile>())!;
        }
        await Check.Run("K04 profile queries require CUSTOMER and ignore foreign account selection", async () =>
        {
            using var anonymous = await guest.Http.GetAsync("/api/account/profile"); using var denied = await adminClient.Http.GetAsync("/api/account/profile");
            Check.True(anonymous.StatusCode == HttpStatusCode.Unauthorized && denied.StatusCode == HttpStatusCode.Forbidden, "Profile role boundaries failed.");
            using var selected = await first.Http.GetAsync("/api/account/profile?accountId=" + secondId);
            var text = await selected.Content.ReadAsStringAsync(); using var json = JsonDocument.Parse(text);
            Check.True(selected.IsSuccessStatusCode && json.RootElement.GetProperty("id").GetInt64() == firstId && !text.Contains("password", StringComparison.OrdinalIgnoreCase), "Profile ID selection leaked another user or credentials.");
            using var overpost = await first.Send("/api/account/profile", new { fullName = valid.FullName, phone = valid.Phone, email = valid.Email, address = valid.Address, id = secondId }, HttpMethod.Put);
            Check.True(overpost.StatusCode == HttpStatusCode.BadRequest && (await Read(second)).FullName == "Second Customer", "Profile actor overposting changed another user.");
        });
        await Check.Run("K04 invalid/duplicate profile fields and missing CSRF preserve all state", async () =>
        {
            var before = await Read(first); await using var db = Open(); var sessions = await db.Sessions.CountAsync(); var hash = (await db.Accounts.AsNoTracking().SingleAsync(x => x.Id == firstId)).PasswordHash;
            foreach (var input in new[] { valid with { FullName = " " }, valid with { FullName = new string('a',121) }, valid with { Phone = "bad" },
                valid with { Phone = "0956789002" }, valid with { Email = "SECOND-PROFILE@EXAMPLE.TEST" }, valid with { Email = "bad" }, valid with { Address = new string('a',501) } })
            {
                using var response = await first.Send("/api/account/profile", input, HttpMethod.Put);
                Check.True(response.StatusCode == HttpStatusCode.BadRequest, "Invalid/duplicate profile accepted.");
            }
            using var csrf = await first.Send("/api/account/profile", valid, HttpMethod.Put, csrf: false);
            Check.True(csrf.StatusCode == HttpStatusCode.BadRequest && await Read(first) == before, "Rejected profile request changed fields.");
            Check.True(await db.Sessions.CountAsync() == sessions && (await db.Accounts.AsNoTracking().SingleAsync(x => x.Id == firstId)).PasswordHash == hash, "Profile mutation changed sessions/password.");
        });
        await Check.Run("K04 valid normalized edits preserve sessions, other customers and order snapshots", async () =>
        {
            using var response = await first.Send("/api/account/profile", valid with { FullName = " Updated Customer ", Email = " NEW-PROFILE@EXAMPLE.TEST ", Address = " New address " }, HttpMethod.Put);
            Check.True(response.StatusCode == HttpStatusCode.NoContent, "Valid profile update failed.");
            var stored = await Read(first);
            Check.True(stored.FullName == valid.FullName && stored.Phone == valid.Phone && stored.Email == valid.Email && stored.Address == valid.Address, "Profile normalization failed.");
            Check.True((await Read(second)).Phone == "0956789002", "Update changed another customer's profile.");
            await using var db = Open(); var snapshot = await db.Orders.SingleAsync(x => x.Id == snapshotOrderId);
            Check.True(snapshot.Recipient == "Snapshot Recipient" && snapshot.Phone == "0956789001" && snapshot.Address == "Snapshot address", "Profile edit rewrote old order snapshots.");
            using var emptyEmail = await first.Send("/api/account/profile", valid with { Email = null }, HttpMethod.Put);
            Check.True(emptyEmail.IsSuccessStatusCode && (await Read(first)).Email is null, "Optional profile email could not be cleared.");
        });
        await Check.Run("K04 concurrent phone claims produce one winner and preserve the losing profile", async () =>
        {
            var beforeA = await Read(first); var beforeB = await Read(second);
            var results = await Task.WhenAll(first.Send("/api/account/profile", valid with { Phone = "0956789099", FullName = "Claim A", Email = null }, HttpMethod.Put),
                second.Send("/api/account/profile", valid with { Phone = "0956789099", FullName = "Claim B", Email = null }, HttpMethod.Put));
            try
            {
                Check.True(results.Count(x => x.StatusCode == HttpStatusCode.NoContent) == 1 && results.Count(x => x.StatusCode == HttpStatusCode.BadRequest) == 1, "Concurrent phone uniqueness failed.");
                if (results[0].StatusCode == HttpStatusCode.BadRequest) Check.True(await Read(first) == beforeA, "Losing profile was partly changed.");
                else Check.True(await Read(second) == beforeB, "Losing profile was partly changed.");
            }
            finally { foreach (var response in results) response.Dispose(); }
        });
        await Check.Run("K04 service rejects a session owned by a different account", async () =>
        {
            await using var db = Open(); var foreign = await db.Sessions.FirstAsync(x => x.AccountId == secondId);
            var profiles = new AccountProfiles(db, TimeProvider.System);
            try { await profiles.ReadAsync(firstId, foreign.Id); throw new InvalidOperationException("Foreign session read a profile."); }
            catch (BusinessException exception) when (exception.Code == "UNAUTHORIZED") { }
            try { await profiles.UpdateAsync(firstId, foreign.Id, valid); throw new InvalidOperationException("Foreign session edited a profile."); }
            catch (BusinessException exception) when (exception.Code == "UNAUTHORIZED") { }
        });
    }
}
