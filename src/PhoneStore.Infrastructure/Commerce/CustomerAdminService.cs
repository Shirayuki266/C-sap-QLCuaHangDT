using Microsoft.EntityFrameworkCore;
using PhoneStore.Core.Accounts;
using PhoneStore.Core.Commerce;
using PhoneStore.Infrastructure.Accounts;
namespace PhoneStore.Infrastructure.Commerce;
public sealed class CustomerAdminService(BusinessKernel kernel)
{
    public Task<Page<AdminCustomer>> List(Actor actor,string? query=null,bool? active=null,int page=1,int pageSize=20) => kernel.Read<Page<AdminCustomer>>(async () =>
    {
        await kernel.Require(actor,"ADMIN");var offset=Rules.Offset(page,pageSize);var term=Rules.Text(query,100,"Từ khóa",true);
        var q=from c in kernel.Db.Customers.AsNoTracking() join a in kernel.Db.Accounts.AsNoTracking() on c.Id equals a.Id
              where a.Role=="CUSTOMER"&&(!active.HasValue||a.Active==active)&&(term==""||c.FullName.Contains(term)||c.Phone.Contains(term)||(c.Email!=null&&c.Email.Contains(term))||a.Username.Contains(term)) select new{c,a};
        var count=await q.CountAsync();var rows=await q.OrderBy(x=>x.c.FullName).ThenBy(x=>x.c.Id).Skip(offset).Take(pageSize).ToListAsync();
        return new(rows.Select(x=>new AdminCustomer(x.c.Id,x.a.Username,x.c.FullName,x.c.Phone,x.c.Email,x.c.Address,x.a.Active,new(DateTime.SpecifyKind(x.a.CreatedAt,DateTimeKind.Utc)))).ToList(),count,page,pageSize);
    });
    public Task<AdminCustomer> Read(Actor actor,long id) => kernel.Read<AdminCustomer>(async () =>
    {
        await kernel.Require(actor,"ADMIN");
        var row=await(from c in kernel.Db.Customers.AsNoTracking() join a in kernel.Db.Accounts.AsNoTracking() on c.Id equals a.Id where c.Id==id&&a.Role=="CUSTOMER" select new{c,a}).SingleOrDefaultAsync()
            ??throw Rules.Error("NOT_FOUND","Không tìm thấy khách hàng.");
        return new(row.c.Id,row.a.Username,row.c.FullName,row.c.Phone,row.c.Email,row.c.Address,row.a.Active,new(DateTime.SpecifyKind(row.a.CreatedAt,DateTimeKind.Utc)));
    });
    public Task<bool> Update(Actor actor,long id,ProfileChangeRequest input)=>kernel.Write(async()=>
    {
        await Read(actor,id);var profile=ProfileInput.Validate(input);
        var customer=await kernel.Db.Customers.SingleAsync(x=>x.Id==id);
        customer.FullName=profile.FullName;customer.Phone=profile.Phone;customer.Email=profile.Email;customer.Address=profile.Address;return true;
    });
    public Task<bool> SetActive(Actor actor,long id,CustomerStateInput input)=>kernel.Write(async()=>
    {
        var current=await Read(actor,id);
        if(current.Active!=input.ExpectedActive)throw Rules.Error("CONCURRENT_UPDATE","Trạng thái khách hàng đã thay đổi. Vui lòng tải lại.");
        if(current.Active==input.Active)throw Rules.Error("INVALID_STATE","Tài khoản đã ở trạng thái yêu cầu.");
        var account=await kernel.Db.Accounts.SingleAsync(x=>x.Id==id);account.Active=input.Active;
        if(!input.Active)await kernel.Db.Sessions.Where(x=>x.AccountId==id).ExecuteDeleteAsync();
        return true;
    });
}
