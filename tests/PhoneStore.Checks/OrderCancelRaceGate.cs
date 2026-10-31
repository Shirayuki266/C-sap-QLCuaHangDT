using System.Data.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace PhoneStore.Checks;

// Test-only: observe two real requests, hold admin's actual SQL application lock,
// then let customer's command block on that lock before admin is allowed to commit.
internal sealed class OrderCancelRaceGate : DbCommandInterceptor
{
    public IHttpContextAccessor Accessor { get; set; } = null!;
    public bool Enabled { get; set; }
    private int arrivals;
    public int Arrivals=>Volatile.Read(ref arrivals);
    public int AdminSpid { get; private set; }
    public int CustomerSpid { get; private set; }
    public TaskCompletionSource Ready { get; }=new(TaskCreationOptions.RunContinuationsAsynchronously);
    public TaskCompletionSource ReleaseAdmin { get; }=new(TaskCreationOptions.RunContinuationsAsynchronously);
    public TaskCompletionSource ReleaseCustomer { get; }=new(TaskCreationOptions.RunContinuationsAsynchronously);
    public TaskCompletionSource AdminLockHeld { get; }=new(TaskCreationOptions.RunContinuationsAsynchronously);
    public TaskCompletionSource ContinueAdmin { get; }=new(TaskCreationOptions.RunContinuationsAsynchronously);
    private bool Target(DbCommand command)=>Enabled&&command.CommandText.Contains("@LockMode='Exclusive'",StringComparison.Ordinal);
    private bool Admin=>Accessor.HttpContext!.Request.Path.StartsWithSegments("/api/admin/orders");
    public override async ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command,CommandEventData data,InterceptionResult<int> result,CancellationToken cancellationToken=default)
    {
        if(!Target(command))return result;
        var connection=(SqlConnection)command.Connection!;var admin=Admin;
        if(admin)AdminSpid=connection.ServerProcessId;else CustomerSpid=connection.ServerProcessId;
        if(Interlocked.Increment(ref arrivals)==2)Ready.TrySetResult();
        await (admin?ReleaseAdmin:ReleaseCustomer).Task.WaitAsync(TimeSpan.FromSeconds(15),cancellationToken);return result;
    }
    public override async ValueTask<int> NonQueryExecutedAsync(DbCommand command,CommandExecutedEventData data,int result,CancellationToken cancellationToken=default)
    {
        if(Target(command)&&Admin){AdminLockHeld.TrySetResult();await ContinueAdmin.Task.WaitAsync(TimeSpan.FromSeconds(15),cancellationToken);}return result;
    }
    public void Unblock(){ReleaseAdmin.TrySetResult();ReleaseCustomer.TrySetResult();ContinueAdmin.TrySetResult();}
    public async Task<bool> ObserveSqlWait(string connection)
    {
        await using var db=new SqlConnection(connection);await db.OpenAsync();await using var command=db.CreateCommand();
        command.CommandText="SELECT COUNT(*) FROM sys.dm_exec_requests WHERE session_id=@customer AND blocking_session_id=@admin AND database_id=DB_ID() AND wait_type LIKE N'LCK_M_%'";
        command.Parameters.AddWithValue("@customer",CustomerSpid);command.Parameters.AddWithValue("@admin",AdminSpid);
        for(var i=0;i<100;i++){if(Convert.ToInt32(await command.ExecuteScalarAsync())==1)return true;await Task.Delay(20);}return false;
    }
}
