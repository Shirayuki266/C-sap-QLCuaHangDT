using Microsoft.EntityFrameworkCore;
using PhoneStore.Core.Commerce;
using PhoneStore.Infrastructure.Commerce;
namespace PhoneStore.Checks;
internal static class CommerceReadChecks
{
    public static async Task Run(Uri address,string connection)
    {
        using var c=new CommerceContext(connection,address,"coherent-reads");await c.Initialize();
        await Check.Run("Read aggregates hold a shared SQL application lock against concurrent business writes",async()=>
        {
            var entered=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);var release=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            await using var first=c.Open();await using var second=c.Open();var k=c.Kernel(first);var writer=c.Kernel(second);var name="Read lock "+Guid.NewGuid().ToString("N");
            var read=k.Read(async()=>{await k.Require(c.Admin,"ADMIN");entered.SetResult();await release.Task;return await first.Brands.CountAsync(x=>x.Name==name);});
            await entered.Task;var write=writer.Write(async()=>{second.Brands.Add(new(){Name=name});return true;});
            // SQL reports the held lock and whether another transaction can acquire it.
            var waiting=false;for(var i=0;i<50;i++)
            {
                await using var observer=c.Open();await using var probe=await observer.Database.BeginTransactionAsync();
                var held=await first.Database.SqlQueryRaw<string>("SELECT APPLOCK_MODE('public','PhoneStore:write','Transaction') AS [Value]").SingleAsync();
                waiting=held=="Shared"&&await observer.Database.SqlQueryRaw<int>("SELECT APPLOCK_TEST('public','PhoneStore:write','Exclusive','Transaction') AS [Value]").SingleAsync()==0&&!write.IsCompleted;
                if(waiting)break;await Task.Delay(20);
            }
            release.SetResult();var count=await read;await write;
            Check.True(waiting&&count==0&&await first.Brands.CountAsync(x=>x.Name==name)==1,"Read/write SQL consistency lock failed.");
        });
    }
}
