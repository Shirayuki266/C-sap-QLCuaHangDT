using System.Collections.Concurrent;
using System.Data.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace PhoneStore.Admin.Client.Checks;

internal sealed class OrderRaceGate:DbCommandInterceptor
{
    public bool Enabled;
    private int arrivals;
    public int Arrivals=>Volatile.Read(ref arrivals);
    public ConcurrentDictionary<Guid,byte> Connections { get; }=new();
    public TaskCompletionSource Ready { get; }=new(TaskCreationOptions.RunContinuationsAsynchronously);
    public TaskCompletionSource ReleaseCustomer { get; }=new(TaskCreationOptions.RunContinuationsAsynchronously);
    public TaskCompletionSource ReleaseAdmin { get; }=new(TaskCreationOptions.RunContinuationsAsynchronously);
    private async Task Wait(DbCommand command,CancellationToken ct)
    {
        if(!Enabled||!command.CommandText.Contains("@LockMode='Exclusive'",StringComparison.Ordinal))return;
        if(command.Connection is SqlConnection connection)Connections.TryAdd(connection.ClientConnectionId,0);
        var customer=new HttpContextAccessor().HttpContext?.Request.Path.StartsWithSegments("/api/orders")==true;
        if(Interlocked.Increment(ref arrivals)==2)Ready.TrySetResult();
        await (customer?ReleaseCustomer:ReleaseAdmin).Task.WaitAsync(TimeSpan.FromSeconds(20),ct);
    }
    public override async ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command,CommandEventData data,InterceptionResult<int> result,CancellationToken ct=default){await Wait(command,ct);return result;}
    public override async ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command,CommandEventData data,InterceptionResult<DbDataReader> result,CancellationToken ct=default){await Wait(command,ct);return result;}
}
