using System.Net;
using System.Net.Http.Json;
using PhoneStore.Core.Commerce;
using PhoneStore.Infrastructure.Commerce;

namespace PhoneStore.Checks;
internal static class LookupChecks
{
    public static async Task Run(Uri address, string connection)
    {
        using var c = new CommerceContext(connection, address, "lookups"); await c.Initialize();
        await Check.Run("A08 both lookup kinds validate uniqueness, disable without deletion and filter pages", async () =>
        {
            foreach (var kind in new[] { "brands", "categories" })
            {
                await using var db = c.Open(); var service = new LookupService(c.Kernel(db));
                var id = await service.Save(c.Admin, kind, 0, new("Lookup " + kind));
                await CommerceContext.Reject("INVALID_INPUT", async () => await service.Save(c.Admin, kind, 0, new("Lookup " + kind)));
                await CommerceContext.Reject("INVALID_INPUT", async () => await service.Save(c.Admin, kind, 0, new(" ")));
                await service.Save(c.Admin, kind, id, new("Disabled " + kind, false));
                Check.True(!(await service.Public(kind)).Any(x => x.Id == id), "Disabled lookup remained public.");
                var internalRows = await service.List(c.Admin, kind, "Disabled", pageSize: 1);
                Check.True(internalRows.Items.Any(x => x.Id == id) && !internalRows.Items[0].Active, "Disabled lookup/history lost.");
                await CommerceContext.Reject("NOT_FOUND", async () => await service.Save(c.Admin, kind, int.MaxValue, new("Missing")));
            }
        });
        await Check.Run("A08 HTTP roles, CSRF, malformed paging and successful writes use real backend", async () =>
        {
            using var forbidden = await c.CustomerClient.Send("/api/admin/lookups/brands", new LookupInput("No permission"));
            using var csrf = await c.AdminClient.Send("/api/admin/lookups/brands", new LookupInput("No CSRF"), csrf: false);
            using var created = await c.AdminClient.Send("/api/admin/lookups/brands", new LookupInput("HTTP brand"));
            using var invalid = await c.AdminClient.Http.GetAsync("/api/admin/lookups/brands?page=0");
            Check.True(forbidden.StatusCode == HttpStatusCode.Forbidden && csrf.StatusCode == HttpStatusCode.BadRequest && created.StatusCode == HttpStatusCode.Created && invalid.StatusCode == HttpStatusCode.BadRequest, "Lookup HTTP boundary failed.");
            using var publicList = await c.CustomerClient.Http.GetAsync("/api/lookups/brands");
            Check.True((await publicList.Content.ReadFromJsonAsync<List<LookupView>>())!.Any(x => x.Name == "HTTP brand"), "Public lookup did not read persisted data.");
        });
    }
}
