using Microsoft.EntityFrameworkCore;
using PhoneStore.Core.Commerce;
namespace PhoneStore.Infrastructure.Commerce;
public sealed class OrderQueryService(BusinessKernel kernel)
{
    public static readonly string[] States=["CHO_XAC_NHAN","DA_XAC_NHAN","DANG_GIAO","HOAN_TAT","DA_HUY","GIAO_THAT_BAI"];
    public Task<OrderView> Read(Actor actor,long id,bool admin=false) => kernel.Read<OrderView>(async () =>
    {
        await kernel.Require(actor,admin?"ADMIN":"CUSTOMER");
        if(!await kernel.Db.Orders.AnyAsync(x=>x.Id==id&&(admin||x.CustomerId==actor.AccountId)))throw Rules.Error("NOT_FOUND","Không tìm thấy đơn hàng.");
        return await OrderReadModel.Load(kernel.Db,id);
    });
    public Task<Page<OrderView>> List(Actor actor,OrderFilter filter,bool admin=false) => kernel.Read<Page<OrderView>>(async () =>
    {
        await kernel.Require(actor,admin?"ADMIN":"CUSTOMER");var offset=Rules.Offset(filter.Page,filter.PageSize);var term=Rules.Text(filter.Query,100,"Từ khóa",true);var dates=DateWindow.Parse(filter.From,filter.To);
        if(filter.Status is not null&&!States.Contains(filter.Status))throw Rules.Error("INVALID_INPUT","Trạng thái đơn hàng không hợp lệ.");
        if(!admin&&filter.CustomerId.HasValue&&filter.CustomerId!=actor.AccountId)throw Rules.Error("FORBIDDEN","Không được chọn đơn của tài khoản khác.");
        if(admin&&filter.CustomerId.HasValue&&!await kernel.Db.Customers.AnyAsync(x=>x.Id==filter.CustomerId))throw Rules.Error("NOT_FOUND","Không tìm thấy khách hàng.");
        var q=kernel.Db.Orders.AsNoTracking().Where(x=>(admin||x.CustomerId==actor.AccountId)&&(!filter.CustomerId.HasValue||x.CustomerId==filter.CustomerId)&&
            (filter.Status==null||x.Status==filter.Status)&&(!dates.Start.HasValue||x.CreatedAt>=dates.Start)&&(!dates.End.HasValue||x.CreatedAt<dates.End)&&
            (term==""||x.Code.Contains(term)||x.Recipient.Contains(term)||x.Phone.Contains(term)||kernel.Db.Customers.Any(c=>c.Id==x.CustomerId&&(c.FullName.Contains(term)||c.Phone.Contains(term)))));
        var count=await q.CountAsync();var ids=await q.OrderByDescending(x=>x.Id).Skip(offset).Take(filter.PageSize).Select(x=>x.Id).ToListAsync();
        var rows=new List<OrderView>();foreach(var id in ids)rows.Add(await OrderReadModel.Load(kernel.Db,id));return new(rows,count,filter.Page,filter.PageSize);
    });
}
