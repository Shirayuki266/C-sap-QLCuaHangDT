using Microsoft.EntityFrameworkCore;
using PhoneStore.Core.Commerce;
using PhoneStore.Infrastructure.Data;
namespace PhoneStore.Infrastructure.Commerce;
public sealed partial class PurchaseService
{
    public Task<bool> Confirm(Actor actor, long id, VersionedCommand command) => kernel.Write(async () =>
    {
        await kernel.Require(actor, "ADMIN"); Rules.Key(command.OperationKey); var db = kernel.Db;
        var receipt = await db.Purchases.SingleOrDefaultAsync(x => x.Id == id) ?? throw Rules.Error("NOT_FOUND", "Không tìm thấy phiếu nhập.");
        Rules.Version(receipt.Version, command.Version);
        if (receipt.Status != "NHAP") throw Rules.Error("INVALID_STATE", "Phiếu không còn ở trạng thái nháp.");
        var lines = await db.PurchaseLines.AsNoTracking().Where(x => x.PurchaseId == id).OrderBy(x => x.VariantId).ToListAsync();
        if (lines.Count is 0 or > 100 || !await db.Suppliers.AnyAsync(x => x.Id == receipt.SupplierId && x.Active)) throw Rules.Error("INVALID_INPUT", "Phiếu rỗng hoặc nhà cung cấp ngừng hoạt động.");
        var total = Rules.Money(lines.Sum(x => x.Quantity * x.Cost));
        if (total != receipt.Total) throw Rules.Error("INVALID_STATE", "Tổng phiếu không khớp chi tiết. Vui lòng kiểm tra dữ liệu.");
        if (await db.Movements.AnyAsync(x => x.OperationKey == command.OperationKey)) throw Rules.Error("INVALID_STATE", "Khóa thao tác đã được sử dụng. Hãy tra cứu kết quả trước khi thử lại.");
        foreach (var line in lines)
        {
            Rules.Quantity(line.Quantity); Rules.Money(line.Cost);
            var variant = await db.Variants.SingleAsync(x => x.Id == line.VariantId);
            var price = Rules.Price(line.Cost, variant.ProfitPercent); var limit = int.MaxValue - line.Quantity;
            if (await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE dbo.TON_KHO SET OnHand=OnHand+{line.Quantity} WHERE VariantId={line.VariantId} AND OnHand<={limit}") != 1)
                throw Rules.Error("INVALID_INPUT", "Tồn kho không hợp lệ hoặc lượng nhập vượt giới hạn lưu trữ.");
            variant.Cost = line.Cost; variant.Price = price; db.Entry(variant).Property(x => x.Active).IsModified = true;
            db.Movements.Add(new StockMovement { VariantId = variant.Id, ActorId = actor.AccountId, PurchaseId = id, Kind = "NHAP", DeltaOnHand = line.Quantity, At = kernel.Now, OperationKey = command.OperationKey });
        }
        receipt.Status = "DA_XAC_NHAN"; receipt.ConfirmerId = actor.AccountId; receipt.ConfirmedAt = kernel.Now; return true;
    });
}
