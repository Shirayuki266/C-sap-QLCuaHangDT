using Microsoft.EntityFrameworkCore;
using PhoneStore.Infrastructure.Data;
using PhoneStore.Web;

var builder = WebApplication.CreateBuilder(args);
var connection = SqlTarget.Validate(builder.Configuration.GetConnectionString("Store"));
builder.Services.AddDbContext<StoreDbContext>(options => options.UseSqlServer(connection));
var app = builder.Build();

app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    try { await next(context); }
    catch (Exception exception) when (!context.Response.HasStarted)
    {
        var error = ApiError.From(exception, context.TraceIdentifier);
        if (error.Code == "SERVER_ERROR")
            app.Logger.LogError(exception, "Request failed: {TraceId}", context.TraceIdentifier);
        context.Response.Clear();
        context.Response.StatusCode = ApiError.Status(error.Code);
        await context.Response.WriteAsJsonAsync(error);
    }
});
app.UseStatusCodePages(async status =>
{
    var context = status.HttpContext;
    var (code, message) = context.Response.StatusCode switch
    {
        404 => ("NOT_FOUND", "Không tìm thấy tài nguyên được yêu cầu."),
        405 => ("METHOD_NOT_ALLOWED", "Phương thức yêu cầu không được hỗ trợ."),
        _ => ("REQUEST_FAILED", "Không thể xử lý yêu cầu.")
    };
    await context.Response.WriteAsJsonAsync(new ApiError(code, message, context.TraceIdentifier));
});

app.MapGet("/health/live", () => Results.Ok(new { application = "PhoneStore", status = "live" }));
app.MapGet("/health/ready", async (StoreDbContext db, HttpContext context) =>
{
    using var deadline = CancellationTokenSource.CreateLinkedTokenSource(context.RequestAborted);
    deadline.CancelAfter(TimeSpan.FromSeconds(3));
    try
    {
        // A database connection alone does not prove that the application schema is installed.
        var ready = await db.Database.SqlQueryRaw<int>(
            "SELECT CASE WHEN OBJECT_ID(N'dbo.__PhoneStoreMigrations') IS NULL THEN 0 ELSE 1 END AS [Value]")
            .SingleAsync(deadline.Token);
        if (ready == 1) return Results.Ok(new { status = "ready" });
    }
    catch (Exception exception) when (exception is not OutOfMemoryException)
    {
        app.Logger.LogWarning("Database readiness check failed: {TraceId}", context.TraceIdentifier);
    }
    return Results.Json(new ApiError("DATABASE_UNAVAILABLE", "CSDL chưa sẵn sàng hoặc chưa được tạo cấu trúc.", context.TraceIdentifier), statusCode: 503);
});

app.Run();
