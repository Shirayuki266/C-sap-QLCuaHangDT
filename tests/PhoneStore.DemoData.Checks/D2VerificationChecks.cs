using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PhoneStore.DemoData.Cli;
using PhoneStore.DemoData.Contracts;
using PhoneStore.DemoData.Persistence;
using PhoneStore.DemoData.Verification;
using PhoneStore.Infrastructure.Data;

namespace PhoneStore.DemoData.Checks;

internal static class D2VerificationChecks
{
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    public static async Task<int> Run(SeedStore store, string manifest, string images, Secrets secrets)
    {
        var passed = 0;
        using var document = JsonDocument.Parse(await File.ReadAllTextAsync(manifest));
        var context = document.RootElement.GetProperty("Manifest").GetProperty("Context").Deserialize<DatasetContext>()!;
        var output = Path.Combine(Path.GetDirectoryName(manifest)!, "verification.json");
        string[] verify = ["--verify-demo-data", "--manifest", manifest, "--images-dir", images, "--output", output];
        await using var db = store.Open();
        var before = BusinessManifest.Hash(await BusinessManifest.Read(db));
        Environment.SetEnvironmentVariable("PHONESTORE_DEMO_ADMIN_PASSWORD", null);
        Environment.SetEnvironmentVariable("PHONESTORE_DEMO_CUSTOMER_PASSWORD", null);
        try
        {
            Require(await Program.RunCli(verify, secrets) == 0, "Read-only verifier requires passwords or failed valid data.");
            Require(before == BusinessManifest.Hash(await BusinessManifest.Read(db)), "Verifier wrote business data.");
            Console.WriteLine("PASS D2 standalone SQL verifier succeeds without secrets and preserves persisted manifest");
            passed++;
            async Task Mutate(string label, Func<Task> change, Func<Task> restore)
            {
                await change();
                try
                {
                    var result = await DatasetVerifier.Run(db, context, images);
                    Require(!result.Valid && result.Errors.Contains(label), "SQL verifier missed: " + label);
                    Require(await Program.RunCli(verify, secrets) != 0, "CLI accepted corruption: " + label);
                    using var report = JsonDocument.Parse(await File.ReadAllTextAsync(output));
                    Require(report.RootElement.GetProperty("Errors").EnumerateArray().Any(x => x.GetString() == label), "CLI report lost SQL invariant: " + label);
                    Console.WriteLine("PASS D2 deliberate QA corruption detected: " + label);
                    passed++;
                }
                finally { await restore(); db.ChangeTracker.Clear(); }
            }
            var variantId = await db.Variants.MinAsync(x => x.Id);
            await Mutate("Inventory/journal balance",
                () => db.Inventory.Where(x => x.VariantId == variantId).ExecuteUpdateAsync(s => s.SetProperty(x => x.OnHand, x => x.OnHand + 1)),
                () => db.Inventory.Where(x => x.VariantId == variantId).ExecuteUpdateAsync(s => s.SetProperty(x => x.OnHand, x => x.OnHand - 1)));
            var orderId = await db.Orders.MinAsync(x => x.Id);
            await Mutate("Order totals/required lines/payment",
                () => db.Orders.Where(x => x.Id == orderId).ExecuteUpdateAsync(s => s.SetProperty(x => x.Total, x => x.Total + 1)),
                () => db.Orders.Where(x => x.Id == orderId).ExecuteUpdateAsync(s => s.SetProperty(x => x.Total, x => x.Total - 1)));
            var line = await db.OrderLines.OrderBy(x => x.Id).FirstAsync();
            await Mutate("Historical order snapshot prices and immutable descriptions",
                async () =>
                {
                    await db.OrderLines.Where(x => x.Id == line.Id).ExecuteUpdateAsync(s => s.SetProperty(x => x.Price, x => x.Price + 1));
                    await db.Orders.Where(x => x.Id == line.OrderId).ExecuteUpdateAsync(s => s.SetProperty(x => x.Total, x => x.Total + line.Quantity));
                    await db.Payments.Where(x => x.OrderId == line.OrderId && x.Status == "DA_THU").ExecuteUpdateAsync(s => s.SetProperty(x => x.Amount, x => x.Amount + line.Quantity));
                },
                async () =>
                {
                    await db.OrderLines.Where(x => x.Id == line.Id).ExecuteUpdateAsync(s => s.SetProperty(x => x.Price, x => x.Price - 1));
                    await db.Orders.Where(x => x.Id == line.OrderId).ExecuteUpdateAsync(s => s.SetProperty(x => x.Total, x => x.Total - line.Quantity));
                    await db.Payments.Where(x => x.OrderId == line.OrderId && x.Status == "DA_THU").ExecuteUpdateAsync(s => s.SetProperty(x => x.Amount, x => x.Amount - line.Quantity));
                });
            var history = await db.OrderHistory.AsNoTracking().Where(x => x.From == "CHO_XAC_NHAN" && x.To == "DA_XAC_NHAN").OrderBy(x => x.Id).FirstAsync();
            var createdAt = await db.Orders.Where(x => x.Id == history.OrderId).Select(x => x.CreatedAt).SingleAsync();
            await Mutate("Strict history timestamps and order creation",
                () => db.OrderHistory.Where(x => x.Id == history.Id).ExecuteUpdateAsync(s => s.SetProperty(x => x.At, createdAt)),
                () => db.OrderHistory.Where(x => x.Id == history.Id).ExecuteUpdateAsync(s => s.SetProperty(x => x.At, history.At)));
            var admin = await db.Accounts.Where(x => x.Role == "ADMIN").Select(x => x.Id).SingleAsync();
            var key1 = Guid.NewGuid(); var key2 = Guid.NewGuid();
            await Mutate("Historical stock never negative or over-reserved",
                async () =>
                {
                    await db.Database.ExecuteSqlInterpolatedAsync($"INSERT dbo.NHAT_KY_KHO(VariantId,ActorId,Kind,DeltaOnHand,DeltaReserved,At,Reason,OperationKey) VALUES({variantId},{admin},'DIEU_CHINH',-1,0,{context.Start.AddMinutes(1)},N'QA deliberate prefix corruption',{key1}),({variantId},{admin},'DIEU_CHINH',1,0,{context.Start.AddMinutes(2)},N'QA balancing suffix',{key2})");
                    Require((await new DataAudit(db).Run()).Valid, "Prefix test did not isolate a gap in old audit.");
                },
                () => db.Movements.Where(x => x.OperationKey == key1 || x.OperationKey == key2).ExecuteDeleteAsync());
            var cart = await db.Cart.AsNoTracking().FirstAsync();
            await Mutate("Future or out-of-horizon timestamps",
                () => db.Cart.Where(x => x.CustomerId == cart.CustomerId && x.VariantId == cart.VariantId).ExecuteUpdateAsync(s => s.SetProperty(x => x.UpdatedAt, context.Cutoff.AddSeconds(1))),
                () => db.Cart.Where(x => x.CustomerId == cart.CustomerId && x.VariantId == cart.VariantId).ExecuteUpdateAsync(s => s.SetProperty(x => x.UpdatedAt, cart.UpdatedAt)));
            Require(await Program.RunCli(verify, secrets) == 0, "QA corruption cleanup did not restore a valid dataset.");
            Console.WriteLine("PASS D2 all corruption fixtures restored; read-only verification passes again");
            passed++;
        }
        finally
        {
            Environment.SetEnvironmentVariable("PHONESTORE_DEMO_ADMIN_PASSWORD", secrets.Admin);
            Environment.SetEnvironmentVariable("PHONESTORE_DEMO_CUSTOMER_PASSWORD", secrets.Customer);
        }
        var today = DateOnly.FromDateTime(DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(7)).DateTime);
        var quarterStart = new DateOnly(today.Year, (today.Month - 1) / 3 * 3 + 1, 1);
        foreach (var date in new[] { new DateOnly(today.Year, today.Month, 1), quarterStart, quarterStart.AddDays(-1) }.Distinct())
        {
            string[] seed = ["--seed-demo-large", "--confirm", "--reset", "--seed", "2611", "--as-of", date.ToString("yyyy-MM-dd"), "--scale", "small", "--images-dir", images, "--output", manifest];
            Require(await Program.RunCli(seed, secrets) == 0, "Boundary-day real service simulation failed.");
            Require(await Program.RunCli(verify, secrets) == 0, "Boundary-day SQL verification failed.");
            Console.WriteLine("PASS D2 real SQL first-of-month/quarter or quarter-end completion window: " + date.ToString("yyyy-MM-dd"));
            passed++;
        }
        return passed;
    }
}
