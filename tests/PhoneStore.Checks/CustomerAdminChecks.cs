using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using PhoneStore.Core.Accounts;
using PhoneStore.Core.Commerce;
using PhoneStore.Infrastructure.Commerce;
namespace PhoneStore.Checks;
internal static class CustomerAdminChecks
{
    public static async Task Run(Uri address,string connection)
    {
        using var c=new CommerceContext(connection,address,"customer-admin");await c.Initialize();await using var db=c.Open();var s=new CustomerAdminService(c.Kernel(db));
        await Check.Run("A09/A10 customer administration is bounded, safe and preserves history/credentials",async()=>
        {
            var old=await s.Read(c.Admin,c.Customer.AccountId);var hash=await db.Accounts.Where(x=>x.Id==old.Id).Select(x=>x.PasswordHash).SingleAsync();var sessions=await db.Sessions.CountAsync(x=>x.AccountId==old.Id);
            await s.Update(c.Admin,old.Id,new("  Tên đã chỉnh  ",old.Phone,"ADMIN-EDIT@EXAMPLE.TEST","Địa chỉ mới"));var updated=await s.Read(c.Admin,old.Id);
            Check.True(updated.FullName=="Tên đã chỉnh"&&updated.Email=="admin-edit@example.test"&&await db.Sessions.CountAsync(x=>x.AccountId==old.Id)==sessions&&await db.Accounts.Where(x=>x.Id==old.Id).Select(x=>x.PasswordHash).SingleAsync()==hash,"Admin profile edit changed credentials or sessions.");
            await CommerceContext.Reject("INVALID_INPUT",async()=>await s.Update(c.Admin,old.Id,new("Bad","wrong",null,"")));
            await CommerceContext.Reject("NOT_FOUND",async()=>await s.Read(c.Admin,c.Admin.AccountId));
            using var denied=await c.CustomerClient.Http.GetAsync("/api/admin/customers");using var page=await c.AdminClient.Http.GetAsync("/api/admin/customers?pageSize=1");var text=await page.Content.ReadAsStringAsync();
            Check.True(denied.StatusCode==HttpStatusCode.Forbidden&&page.IsSuccessStatusCode&&!text.Contains("password",StringComparison.OrdinalIgnoreCase),"Customer list role/secret boundary failed.");
        });
        await Check.Run("A11 lock revokes every session atomically; unlock never resurrects old cookies",async()=>
        {
            using var lockResult=await c.AdminClient.Send($"/api/admin/customers/{c.Customer.AccountId}/state",new CustomerStateInput(false,true));Check.True(lockResult.StatusCode==HttpStatusCode.NoContent,"Lock failed.");
            Check.True(!await db.Sessions.AnyAsync(x=>x.AccountId==c.Customer.AccountId),"Lock preserved a SQL session.");
            using var old=await c.CustomerClient.Http.GetAsync("/api/auth/session");Check.True(old.StatusCode==HttpStatusCode.Unauthorized,"Locked old cookie remained usable.");
            using var fresh=new ApiTestClient(address);using var refused=await fresh.Send("/api/auth/login",new LoginRequest("customer-admin-customer",CommerceContext.Password));Check.True(refused.StatusCode==HttpStatusCode.Unauthorized,"Locked customer logged in.");
            using var unlock=await c.AdminClient.Send($"/api/admin/customers/{c.Customer.AccountId}/state",new CustomerStateInput(true,false));Check.True(unlock.IsSuccessStatusCode,"Unlock failed.");
            using var oldAgain=await c.CustomerClient.Http.GetAsync("/api/auth/session");using var login=await fresh.Send("/api/auth/login",new LoginRequest("customer-admin-customer",CommerceContext.Password));
            Check.True(oldAgain.StatusCode==HttpStatusCode.Unauthorized&&login.IsSuccessStatusCode,"Unlock resurrected old session or denied fresh credentials.");
        });
    }
}
