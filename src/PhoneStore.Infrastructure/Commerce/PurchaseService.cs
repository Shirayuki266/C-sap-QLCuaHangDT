using Microsoft.EntityFrameworkCore;
using PhoneStore.Core.Commerce;
using PhoneStore.Infrastructure.Data;
namespace PhoneStore.Infrastructure.Commerce;
public sealed partial class PurchaseService(BusinessKernel kernel)
{
    public async Task<PurchaseView> Read(Actor actor, long id)
    {
        await kernel.Require(actor, "ADMIN");
        var row = await (from p in kernel.Db.Purchases.AsNoTracking() join supplier in kernel.Db.Suppliers on p.SupplierId equals supplier.Id
                         where p.Id == id select new { p, Supplier = supplier.Name }).SingleOrDefaultAsync()
            ?? throw Rules.Error("NOT_FOUND", "Không tìm thấy phiếu nhập.");
        var lines = await (from l in kernel.Db.PurchaseLines.AsNoTracking() join v in kernel.Db.Variants on l.VariantId equals v.Id
                           join p in kernel.Db.Products on v.ProductId equals p.Id where l.PurchaseId == id orderby l.Id
                           select new PurchaseLineView(v.Id, v.Sku, p.Name, l.Quantity, l.Cost, l.Quantity * l.Cost)).ToListAsync();
        var receipt = row.p;
        return new(receipt.Id, receipt.Code, receipt.SupplierId, row.Supplier, receipt.Status, receipt.Total, receipt.CreatorId, receipt.ConfirmerId,
            new(DateTime.SpecifyKind(receipt.CreatedAt, DateTimeKind.Utc)), receipt.ConfirmedAt.HasValue ? new DateTimeOffset(DateTime.SpecifyKind(receipt.ConfirmedAt.Value, DateTimeKind.Utc)) : null,
            Convert.ToBase64String(receipt.Version), lines);
    }
    public async Task<Page<PurchaseView>> List(Actor actor, string? query = null, string? status = null, int page = 1, int pageSize = 20)
    {
        await kernel.Require(actor, "ADMIN"); var term = Rules.Text(query, 100, "Từ khóa", true); Rules.Offset(page, pageSize);
        if (status is not null && status is not ("NHAP" or "DA_XAC_NHAN" or "DA_HUY")) throw Rules.Error("INVALID_INPUT", "Trạng thái phiếu nhập không hợp lệ.");
        var q = kernel.Db.Purchases.AsNoTracking().Where(x => (status == null || x.Status == status) && (term == "" || x.Code.Contains(term) || kernel.Db.Suppliers.Any(s => s.Id == x.SupplierId && s.Name.Contains(term))));
        var count = await q.CountAsync(); var ids = await q.OrderByDescending(x => x.Id).Skip(Rules.Offset(page, pageSize)).Take(pageSize).Select(x => x.Id).ToListAsync();
        var rows = new List<PurchaseView>(); foreach (var id in ids) rows.Add(await Read(actor, id)); return new(rows, count, page, pageSize);
    }
    public Task<long> Save(Actor actor, long id, PurchaseInput input) => kernel.Write(async () =>
    {
        await kernel.Require(actor, "ADMIN"); var db = kernel.Db;
        if (id < 0 || input.Lines is null || input.Lines.Count > 100 || input.Lines.Any(x => x is null) || input.Lines.Select(x => x.VariantId).Distinct().Count() != input.Lines.Count)
            throw Rules.Error("INVALID_INPUT", "Phiếu nhập không hợp lệ hoặc có dòng biến thể bị lặp.");
        foreach (var l in input.Lines) { Rules.Quantity(l.Quantity); Rules.Money(l.Cost); }
        var total = Rules.Money(input.Lines.Sum(x => x.Quantity * x.Cost), true);
        if (id == 0)
        {
            Rules.Key(input.RequestKey); var code = "PN" + input.RequestKey.ToString("N");
            var existing = await db.Purchases.AsNoTracking().SingleOrDefaultAsync(x => x.Code == code);
            if (existing is not null)
            {
                var old = await db.PurchaseLines.AsNoTracking().Where(x => x.PurchaseId == existing.Id).OrderBy(x => x.VariantId).Select(x => new PurchaseLineInput(x.VariantId, x.Quantity, x.Cost)).ToListAsync();
                if (existing.CreatorId != actor.AccountId || existing.SupplierId != input.SupplierId || !old.SequenceEqual(input.Lines.OrderBy(x => x.VariantId)))
                    throw Rules.Error("INVALID_STATE", "Khóa tạo phiếu đã dùng cho nội dung khác. Hãy tra cứu phiếu trước khi gửi lại.");
                return existing.Id;
            }
        }
        var row = id == 0 ? new Purchase { CreatorId = actor.AccountId, Code = "PN" + input.RequestKey.ToString("N"), CreatedAt = kernel.Now } :
            await db.Purchases.SingleOrDefaultAsync(x => x.Id == id) ?? throw Rules.Error("NOT_FOUND", "Không tìm thấy phiếu nhập.");
        if (id != 0) Rules.Version(row.Version, input.Version);
        if (row.Status != "NHAP") throw Rules.Error("INVALID_STATE", "Chỉ được sửa phiếu đang nháp.");
        if (!await db.Suppliers.AnyAsync(x => x.Id == input.SupplierId && x.Active)) throw Rules.Error("INVALID_INPUT", "Nhà cung cấp không hoạt động.");
        var ids = input.Lines.Select(x => x.VariantId).ToList();
        if (await db.Variants.CountAsync(x => ids.Contains(x.Id)) != ids.Count) throw Rules.Error("INVALID_INPUT", "Biến thể nhập không tồn tại.");
        row.SupplierId = input.SupplierId; row.Total = total;
        if (id == 0) db.Purchases.Add(row); else db.Entry(row).Property(x => x.Status).IsModified = true;
        await db.SaveChangesAsync();
        await db.PurchaseLines.Where(x => x.PurchaseId == row.Id).ExecuteDeleteAsync();
        db.PurchaseLines.AddRange(input.Lines.Select(x => new PurchaseLine { PurchaseId = row.Id, VariantId = x.VariantId, Quantity = x.Quantity, Cost = x.Cost }));
        return row.Id;
    });
    public Task<bool> Cancel(Actor actor, long id, VersionedCommand command) => kernel.Write(async () =>
    {
        await kernel.Require(actor, "ADMIN"); Rules.Key(command.OperationKey);
        var row = await kernel.Db.Purchases.SingleOrDefaultAsync(x => x.Id == id) ?? throw Rules.Error("NOT_FOUND", "Không tìm thấy phiếu nhập.");
        Rules.Version(row.Version, command.Version);
        if (row.Status != "NHAP") throw Rules.Error("INVALID_STATE", "Chỉ được hủy phiếu nháp.");
        row.Status = "DA_HUY"; return true;
    });
}
