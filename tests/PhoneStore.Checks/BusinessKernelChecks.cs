using Microsoft.EntityFrameworkCore;
using PhoneStore.Infrastructure.Commerce;
using PhoneStore.Infrastructure.Data;

namespace PhoneStore.Checks;

internal static class BusinessKernelChecks
{
    public static async Task Run(Uri address, string connection)
    {
        using var context = new CommerceContext(connection, address, "kernel"); await context.Initialize();
        await Check.Run("Commerce kernel validates live SQL actor roles and session ownership", async () =>
        {
            await using var db = context.Open(); var kernel = context.Kernel(db);
            await kernel.Require(context.Admin, "ADMIN");
            await CommerceContext.Reject("FORBIDDEN", async () => await kernel.Require(context.Customer, "ADMIN"));
            await CommerceContext.Reject("UNAUTHORIZED", async () => await kernel.Require(context.Admin with { SessionId = context.Customer.SessionId }, "ADMIN"));
        });
        await Check.Run("Commerce kernel rolls back nested writes and maps SQL uniqueness errors", async () =>
        {
            await using var db = context.Open(); var kernel = context.Kernel(db);
            await CommerceContext.Reject("INVALID_INPUT", async () => await kernel.Write(async () =>
            {
                db.Brands.Add(new Brand { Name = "Kernel rollback" }); await db.SaveChangesAsync();
                return await kernel.Write(async () => { db.Brands.Add(new Brand { Name = "Kernel rollback" }); await db.SaveChangesAsync(); return true; });
            }));
            Check.True(!await db.Brands.AnyAsync(x => x.Name == "Kernel rollback"), "Partial write survived rollback.");
        });
        await Check.Run("Commerce numeric/version/paging rules match SQL storage and report rounding", () =>
        {
            Check.True(Rules.Price(1m, 0.5m) == 1.01m, "Price did not round midpoint AwayFromZero.");
            Check.Reject<PhoneStore.Core.BusinessException>(() => Rules.Price(Rules.MaxMoney, 10000m));
            Check.Reject<PhoneStore.Core.BusinessException>(() => Rules.Price(10, -1));
            Check.Reject<PhoneStore.Core.BusinessException>(() => Rules.Version(new byte[8], "bad"));
            Check.Reject<PhoneStore.Core.BusinessException>(() => Rules.Offset(int.MaxValue, 100));
            return Task.CompletedTask;
        });
    }
}
