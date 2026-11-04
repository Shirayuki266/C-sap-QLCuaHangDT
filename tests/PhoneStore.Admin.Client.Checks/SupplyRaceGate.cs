using System.Collections.Concurrent;
using System.Data.Common;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace PhoneStore.Admin.Client.Checks;

// Test-host-only barrier before BOTH requests acquire the real exclusive SQL application lock.
internal sealed class SupplyRaceGate:DbCommandInterceptor
{
    public bool Enabled;
    public bool FailStockReads,StockFailureSeen;
    private int arrivals;
    public int Arrivals=>Volatile.Read(ref arrivals);
    public ConcurrentDictionary<Guid,byte> Connections { get; }=new();
    public TaskCompletionSource Ready { get; }=new(TaskCreationOptions.RunContinuationsAsynchronously);
    public TaskCompletionSource Release { get; }=new(TaskCreationOptions.RunContinuationsAsynchronously);
    private async Task Wait(DbCommand command,CancellationToken ct)
    {
        if(!Enabled||!command.CommandText.Contains("@LockMode='Exclusive'",StringComparison.Ordinal))return;
        if(command.Connection is SqlConnection connection)Connections.TryAdd(connection.ClientConnectionId,0);
        if(Interlocked.Increment(ref arrivals)==2)Ready.TrySetResult();await Release.Task.WaitAsync(TimeSpan.FromSeconds(15),ct);
    }
    public override async ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command,CommandEventData data,InterceptionResult<int> result,CancellationToken ct=default){await Wait(command,ct);return result;}
    public override async ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command,CommandEventData data,InterceptionResult<DbDataReader> result,CancellationToken ct=default){if(FailStockReads&&command.CommandText.Contains("[TON_KHO]",StringComparison.Ordinal)){StockFailureSeen=true;command.CommandText="THROW 51001,N'QA stock read failure',1;";}await Wait(command,ct);return result;}
}
