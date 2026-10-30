using Microsoft.EntityFrameworkCore;
using PhoneStore.Core.Commerce;
using PhoneStore.Infrastructure.Data;

namespace PhoneStore.Infrastructure.Commerce;

public sealed class LookupService(BusinessKernel kernel)
{
    public async Task<List<LookupView>> Public(string kind)
        => await Query(kind).Where(x => x.Active).OrderBy(x => x.Name).ThenBy(x => x.Id).ToListAsync();
    private IQueryable<LookupView> Query(string kind) => kind switch
    {
        "brands" => kernel.Db.Brands.AsNoTracking().Select(x => new LookupView { Id = x.Id, Name = x.Name, Active = x.Active }),
        "categories" => kernel.Db.Categories.AsNoTracking().Select(x => new LookupView { Id = x.Id, Name = x.Name, Active = x.Active }),
        _ => throw Rules.Error("NOT_FOUND", "Không tìm thấy loại danh mục.")
    };
    public Task<Page<LookupView>> List(Actor actor, string kind, string? query = null, int page = 1, int pageSize = 20) => kernel.Read<Page<LookupView>>(async () =>
    {
        await kernel.Require(actor, "ADMIN"); var offset = Rules.Offset(page, pageSize); var text = Rules.Text(query, 100, "Từ khóa", true);
        var rows = Query(kind); if (text.Length != 0) rows = rows.Where(x => x.Name.Contains(text));
        return new(await rows.OrderBy(x => x.Name).ThenBy(x => x.Id).Skip(offset).Take(pageSize).ToListAsync(), await rows.CountAsync(), page, pageSize);
    });
    public Task<int> Save(Actor actor, string kind, int id, LookupInput input) => kernel.Write(async () =>
    {
        await kernel.Require(actor, "ADMIN"); if (id < 0) throw Rules.Error("INVALID_INPUT", "Mã danh mục không hợp lệ.");
        var name = Rules.Text(input.Name, 100, "Tên danh mục");
        if (kind == "brands")
        {
            var row = id == 0 ? new Brand() : await kernel.Db.Brands.FindAsync(id) ?? throw Rules.Error("NOT_FOUND", "Không tìm thấy hãng.");
            row.Name = name; row.Active = input.Active; if (id == 0) kernel.Db.Add(row); await kernel.Db.SaveChangesAsync(); return row.Id;
        }
        if (kind == "categories")
        {
            var row = id == 0 ? new Category() : await kernel.Db.Categories.FindAsync(id) ?? throw Rules.Error("NOT_FOUND", "Không tìm thấy danh mục.");
            row.Name = name; row.Active = input.Active; if (id == 0) kernel.Db.Add(row); await kernel.Db.SaveChangesAsync(); return row.Id;
        }
        throw Rules.Error("NOT_FOUND", "Không tìm thấy loại danh mục.");
    });
}
