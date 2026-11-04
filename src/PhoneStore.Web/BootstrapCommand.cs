using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using PhoneStore.Infrastructure.Data;
namespace PhoneStore.Web;
public static class BootstrapCommand
{
    public static async Task Run(string mode,IServiceProvider services)
    {
        await using var scope=services.CreateAsyncScope();var db=scope.ServiceProvider.GetRequiredService<StoreDbContext>();var hasher=scope.ServiceProvider.GetRequiredService<IPasswordHasher<Account>>();
        if(mode=="--provision-admin")
        {
            var id=await new AdminProvisioner(db,hasher).Create(Environment.GetEnvironmentVariable("PHONESTORE_ADMIN_USERNAME"),Environment.GetEnvironmentVariable("PHONESTORE_ADMIN_PASSWORD"));
            Console.WriteLine($"Đã tạo ADMIN #{id}. Không hiển thị hoặc lưu mật khẩu vào log.");
        }
        else
        {
            var audit=mode=="--seed-demo"?await new DemoSeeder(db,hasher).Seed(Environment.GetEnvironmentVariable("PHONESTORE_DEMO_ADMIN_PASSWORD"),Environment.GetEnvironmentVariable("PHONESTORE_DEMO_CUSTOMER_PASSWORD")):await new DataAudit(db).Run();
            Console.WriteLine(JsonSerializer.Serialize(audit,new JsonSerializerOptions{WriteIndented=true}));
            if(!audit.Valid)throw new InvalidOperationException("Dữ liệu không đạt kiểm tra nghiệp vụ.");
        }
    }
}
