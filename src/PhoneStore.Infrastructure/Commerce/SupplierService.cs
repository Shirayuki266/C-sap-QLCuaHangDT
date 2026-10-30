using Microsoft.EntityFrameworkCore;
using PhoneStore.Core.Commerce;
using PhoneStore.Infrastructure.Data;
namespace PhoneStore.Infrastructure.Commerce;
public sealed class SupplierService(BusinessKernel kernel)
{
    public Task<Page<SupplierView>> List(Actor actor, string? query = null, bool? active = null, int page = 1, int pageSize = 20) => kernel.Read<Page<SupplierView>>(async () =>
    {
        await kernel.Require(actor, "ADMIN"); var offset = Rules.Offset(page, pageSize); var term = Rules.Text(query, 100, "Từ khóa", true);
        var rows = kernel.Db.Suppliers.AsNoTracking().Where(x => (!active.HasValue || x.Active == active) &&
            (term == "" || x.Code.Contains(term) || x.Name.Contains(term) || x.Phone.Contains(term)));
        return new(await rows.OrderBy(x => x.Name).ThenBy(x => x.Id).Skip(offset).Take(pageSize).Select(x => new SupplierView(x.Id, x.Code, x.Name, x.Phone, x.Address, x.Active)).ToListAsync(), await rows.CountAsync(), page, pageSize);
    });
    public Task<long> Save(Actor actor, long id, SupplierInput input) => kernel.Write(async () =>
    {
        await kernel.Require(actor, "ADMIN"); if (id < 0) throw Rules.Error("INVALID_INPUT", "Mã nhà cung cấp không hợp lệ.");
        var row = id == 0 ? new Supplier() : await kernel.Db.Suppliers.SingleOrDefaultAsync(x => x.Id == id) ?? throw Rules.Error("NOT_FOUND", "Không tìm thấy nhà cung cấp.");
        row.Code = Rules.Text(input.Code, 30, "Mã nhà cung cấp").ToUpperInvariant(); row.Name = Rules.Text(input.Name, 200, "Tên nhà cung cấp");
        row.Phone = Rules.Phone(input.Phone); row.Address = Rules.Text(input.Address, 500, "Địa chỉ"); row.Active = input.Active;
        if (id == 0) kernel.Db.Suppliers.Add(row); await kernel.Db.SaveChangesAsync(); return row.Id;
    });
}
