using System.Data;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PhoneStore.Core.Contracts;
using PhoneStore.Core.Models;
using PhoneStore.Infrastructure.Data;
namespace PhoneStore.Infrastructure.Services;

public sealed partial class StoreService(StoreDbContext db) : IStoreService
{
    private static BusinessException Error(string code,string message)=>new(code,message);
    private static string Text(string? value,int max,string label,bool optional=false) {
        var text=value?.Trim()??"";
        if((!optional && text.Length==0)||text.Length>max) throw Error("INVALID_INPUT",$"{label} không hợp lệ (tối đa {max} ký tự).");
        return text;
    }
    private static string Phone(string? phone) { var value=Text(phone,20,"Số điện thoại"); if(!Regex.IsMatch(value,@"^\+?[0-9]{9,15}$")) throw Error("INVALID_INPUT","Số điện thoại cần 9–15 chữ số."); return value; }
    private static string? Email(string? email) { if(string.IsNullOrWhiteSpace(email)) return null; var value=Text(email,254,"Email").ToLowerInvariant(); if(!MailAddress.TryCreate(value,out var parsed)||parsed.Address!=value) throw Error("INVALID_INPUT","Email không hợp lệ."); return value; }
    private static void Password(string value) { if(value is null || value.Length<10||value.Length>128||!value.Any(char.IsLetter)||!value.Any(char.IsDigit)) throw Error("INVALID_INPUT","Mật khẩu cần 10–128 ký tự, gồm chữ và số."); }
    private static void Money(decimal value) { if(value<=0||value>999999999999m||decimal.Round(value,2)!=value) throw Error("INVALID_INPUT","Giá phải lớn hơn 0, tối đa 999.999.999.999 và tối đa 2 chữ số thập phân."); }
    private static void Quantity(int value) { if(value<=0||value>10000) throw Error("INVALID_INPUT","Số lượng phải từ 1 đến 10.000."); }
    private static void Version(byte[] actual,string? expected) { if(expected is null||Convert.ToBase64String(actual)!=expected) throw Error("CONCURRENT_UPDATE","Dữ liệu đã thay đổi. Vui lòng tải lại trước khi thao tác."); }
    private static void Key(Guid key) { if(key==Guid.Empty) throw Error("INVALID_INPUT","Thiếu khóa yêu cầu."); }
    private async Task<T> Atomic<T>(Func<Task<T>> action) {
        await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try {
            // Cross-process transaction-owned mutex; all application writes share this lock.
            await db.Database.ExecuteSqlRawAsync("DECLARE @r int; EXEC @r=sp_getapplock @Resource=N'PhoneStore:write',@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=10000; IF @r<0 THROW 51000,N'Không thể khóa giao dịch',1;");
            var result=await action(); await db.SaveChangesAsync(); await tx.CommitAsync(); return result;
        } catch(DbUpdateConcurrencyException) { await tx.RollbackAsync(); db.ChangeTracker.Clear(); throw Error("CONCURRENT_UPDATE","Dữ liệu đã thay đổi. Vui lòng tải lại."); }
        catch(DbUpdateException ex) when (ex.InnerException is Microsoft.Data.SqlClient.SqlException { Number: 2601 or 2627 }) { await tx.RollbackAsync(); db.ChangeTracker.Clear(); throw Error("INVALID_INPUT","Mã, SKU, tài khoản, email hoặc số điện thoại đã tồn tại."); }
        catch { await tx.RollbackAsync(); db.ChangeTracker.Clear(); throw; }
    }
    private async Task<Account> Require(long id,string? role=null) {
        var user=await db.Accounts.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==id);
        if(user is null||!user.Active) throw Error("UNAUTHORIZED","Phiên đăng nhập không hợp lệ hoặc tài khoản đã bị khóa.");
        if(role!=null&&user.Role!=role) throw Error("FORBIDDEN","Bạn không có quyền thực hiện chức năng này."); return user;
    }
    public async Task<UserDto> GetUser(long id) {
        var a=await Require(id); var c=await db.Customers.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==id);
        return new(a.Id,a.Username,a.Role,c?.FullName??"Quản trị viên",c?.Phone??"",c?.Email,c?.Address);
    }
    public Task<long> Register(RegisterRequest input)=>Atomic(async()=> {
        var username=Text(input.Username,100,"Tên đăng nhập").ToLowerInvariant();
        if(!Regex.IsMatch(username,@"^[a-z0-9_.@-]{3,100}$")) throw Error("INVALID_INPUT","Tên đăng nhập cần ít nhất 3 ký tự Latin, số hoặc _ . @ -.");
        Password(input.Password);
        var a=new Account{Username=username}; a.PasswordHash=new PasswordHasher<Account>().HashPassword(a,input.Password);
        var customer=new Customer{Account=a,FullName=Text(input.FullName,120,"Họ tên"),Phone=Phone(input.Phone),Email=Email(input.Email),Address=Text(input.Address,500,"Địa chỉ",true)};
        db.Customers.Add(customer); await db.SaveChangesAsync(); return a.Id;
    });
    public Task<(UserDto User,Guid Session)> Login(LoginRequest input,bool admin=false)=>Atomic(async()=> {
        var username=Text(input.Username,100,"Tên đăng nhập").ToLowerInvariant();
        var a=await db.Accounts.SingleOrDefaultAsync(x=>x.Username==username);
        if(a is null||input.Password is null||input.Password.Length>128||new PasswordHasher<Account>().VerifyHashedPassword(a,a.PasswordHash,input.Password)==PasswordVerificationResult.Failed||(admin&&a.Role!="ADMIN"))
            throw Error("UNAUTHORIZED","Tên đăng nhập hoặc mật khẩu không đúng, hoặc tài khoản không được phép đăng nhập.");
        if(!a.Active)throw Error("UNAUTHORIZED","Tài khoản đã bị khóa. Vui lòng liên hệ quản trị viên.");
        var session=new LoginSession{AccountId=a.Id,ExpiresAt=DateTime.UtcNow.AddHours(8)}; session.Id=Guid.NewGuid(); db.Sessions.Add(session);
        db.Sessions.RemoveRange(await db.Sessions.Where(x=>x.ExpiresAt<DateTime.UtcNow).ToListAsync());
        return(await GetUser(a.Id),session.Id);
    });
    public Task<bool> Logout(long id,Guid session)=>Atomic(async()=> { await Require(id); var row=await db.Sessions.SingleOrDefaultAsync(x=>x.Id==session&&x.AccountId==id); if(row!=null) db.Sessions.Remove(row); return true; });
    public Task<bool> ChangePassword(long id,PasswordRequest input)=>Atomic(async()=> {
        await Require(id); var a=await db.Accounts.SingleAsync(x=>x.Id==id); Password(input.NewPassword);
        if(input.CurrentPassword is null||input.CurrentPassword.Length>128||new PasswordHasher<Account>().VerifyHashedPassword(a,a.PasswordHash,input.CurrentPassword)==PasswordVerificationResult.Failed) throw Error("INVALID_INPUT","Mật khẩu hiện tại không đúng.");
        a.PasswordHash=new PasswordHasher<Account>().HashPassword(a,input.NewPassword); db.Sessions.RemoveRange(await db.Sessions.Where(x=>x.AccountId==id).ToListAsync()); return true;
    });
    public Task<bool> UpdateProfile(long id,ProfileRequest input)=>Atomic(async()=> { await Require(id,"CUSTOMER"); var c=await db.Customers.SingleAsync(x=>x.Id==id); SetProfile(c,input); return true; });
    private static void SetProfile(Customer c,ProfileRequest i) { c.FullName=Text(i.FullName,120,"Họ tên"); c.Phone=Phone(i.Phone); c.Email=Email(i.Email); c.Address=Text(i.Address,500,"Địa chỉ",true); }
}


