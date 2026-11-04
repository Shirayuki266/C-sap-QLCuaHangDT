using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PhoneStore.Infrastructure.Accounts;
using PhoneStore.Infrastructure.Commerce;
namespace PhoneStore.Infrastructure.Data;
public sealed class AdminProvisioner(StoreDbContext db,IPasswordHasher<Account> hasher)
{
    public Task<long> Create(string? username,string? password)=>new BusinessKernel(db,TimeProvider.System).Write(async()=>
    {
        if(!await new DatabaseMigrator(db).IsCurrentAsync())throw new InvalidOperationException("CSDL chưa có cấu trúc hợp lệ.");
        var name=Rules.Text(username,100,"Tên đăng nhập").ToLowerInvariant();
        if(!Regex.IsMatch(name,@"\A[a-z0-9_.@-]{3,100}\z",RegexOptions.NonBacktracking))throw Rules.Error("INVALID_INPUT","Tên đăng nhập không hợp lệ.");
        var secret=PasswordInput.Validate(password);
        if(await db.Accounts.AnyAsync(x=>x.Username==name))throw Rules.Error("INVALID_INPUT","Tài khoản đã tồn tại. Không ghi đè hoặc đổi quyền tài khoản.");
        var account=new Account{Username=name,Role="ADMIN"};account.PasswordHash=hasher.HashPassword(account,secret);db.Add(account);await db.SaveChangesAsync();return account.Id;
    });
}
