using System.Net;
using PhoneStore.Core.Commerce;
using PhoneStore.Infrastructure.Commerce;
namespace PhoneStore.Checks;
internal static class SupplierChecks
{
    public static async Task Run(Uri address, string connection)
    {
        using var c = new CommerceContext(connection, address, "suppliers"); await c.Initialize();
        await using var db = c.Open(); var service = new SupplierService(c.Kernel(db));
        var input = new SupplierInput("SUP-TEST", "Nhà cung cấp thử", "0901234567", "Địa chỉ thử");
        await Check.Run("A13 supplier persistence, duplicates, validation and disabled history are enforced", async () =>
        {
            var id = await service.Save(c.Admin, 0, input);
            await CommerceContext.Reject("INVALID_INPUT", async () => await service.Save(c.Admin, 0, input));
            await CommerceContext.Reject("INVALID_INPUT", async () => await service.Save(c.Admin, id, input with { Phone="bad" }));
            await service.Save(c.Admin, id, input with { Active=false });
            Check.True((await service.List(c.Admin, "SUP-TEST")).Items.Single().Id == id && (await service.List(c.Admin, "SUP-TEST", true)).Total == 0, "Disabled supplier/history was lost or filtered incorrectly.");
            await CommerceContext.Reject("NOT_FOUND", async () => await service.Save(c.Admin, long.MaxValue, input));
        });
        await Check.Run("A13 supplier HTTP authorization and CSRF reject unauthorized writes", async () =>
        {
            using var denied = await c.CustomerClient.Send("/api/admin/suppliers", input);
            using var csrf = await c.AdminClient.Send("/api/admin/suppliers", input, csrf:false);
            using var success = await c.AdminClient.Send("/api/admin/suppliers", input with { Code="SUP-HTTP" });
            Check.True(denied.StatusCode == HttpStatusCode.Forbidden && csrf.StatusCode == HttpStatusCode.BadRequest && success.StatusCode == HttpStatusCode.Created, "Supplier HTTP boundary failed.");
        });
    }
}
