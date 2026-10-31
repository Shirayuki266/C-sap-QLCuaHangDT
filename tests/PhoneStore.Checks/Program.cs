using PhoneStore.Checks;
using PhoneStore.Core;
using PhoneStore.Infrastructure.Data;
using PhoneStore.Web;

if(args.SequenceEqual(new[]{"--prompt2a"}))
{
    await Prompt2AChecks.RunIsolated();Check.Summary();return;
}

if(args.SequenceEqual(new[]{"--priority-p0"}))
{
    await PriorityP0Checks.RunIsolated();Check.Summary();return;
}

if (args.Any(argument => argument != "--integration"))
    throw new ArgumentException("Unknown check argument.");

await Check.Run("SQL configuration requires an explicit application database", () =>
{
    foreach (var invalid in new string?[] { null, "", "Server=localhost", "Database=PhoneStore_Rebuild",
        "Server=localhost;Database=MASTER", "Server=localhost;Database=tempdb", "not-a-connection;Password=private-value" })
        Check.Reject<InvalidOperationException>(() => SqlTarget.Validate(invalid));
    Check.True(SqlTarget.Validate("Server=localhost;Database=PhoneStore_Rebuild;Integrated Security=True").Contains("PhoneStore_Rebuild"), "Valid explicit target was rejected.");
    return Task.CompletedTask;
});
await Check.Run("API errors hide unexpected details and retain business conflicts", () =>
{
    var failure = ApiError.From(new InvalidOperationException("Password=private-value"), "trace-1");
    Check.True(failure.Code == "SERVER_ERROR" && !failure.Message.Contains("private-value") && failure.TraceId == "trace-1", "Unexpected error leaked details.");
    var conflict = ApiError.From(new BusinessException("CONCURRENT_UPDATE", "Vui lòng tải lại."), "trace-2");
    Check.True(ApiError.Status(conflict.Code) == 409 && conflict.Message == "Vui lòng tải lại.", "Business conflict contract changed.");
    return Task.CompletedTask;
});
await HttpChecks.Run();
if (args.Contains("--integration")) await SqlChecks.Run();
Check.Summary();
