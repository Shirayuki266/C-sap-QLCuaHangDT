using System.Collections.Concurrent;
using System.Data.Common;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace PhoneStore.Checks;

// WAF-only interceptor: no shipping hook, timing sleep or serial Task.WhenAll imitation.
internal sealed class CheckoutRaceGate : DbCommandInterceptor
{
    public bool Enabled { get; set; }
    private int arrivals;
    public int Arrivals => Volatile.Read(ref arrivals);
    public ConcurrentDictionary<Guid, byte> Connections { get; } = new();
    public TaskCompletionSource Ready { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private async Task Wait(DbCommand command, CancellationToken cancellationToken)
    {
        if (!Enabled || !command.CommandText.Contains("@LockMode='Exclusive'", StringComparison.Ordinal)) return;
        if(command.Connection is SqlConnection sql) Connections.TryAdd(sql.ClientConnectionId, 0);
        if(Interlocked.Increment(ref arrivals)==2) Ready.TrySetResult();
        await Release.Task.WaitAsync(TimeSpan.FromSeconds(15), cancellationToken);
    }
    public override async ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command, CommandEventData data, InterceptionResult<int> result, CancellationToken cancellationToken=default)
    { await Wait(command,cancellationToken);return result; }
    public override async ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData data, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken=default)
    { await Wait(command,cancellationToken);return result; }
}
