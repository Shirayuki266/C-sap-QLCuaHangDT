using Microsoft.Data.SqlClient;

namespace PhoneStore.Infrastructure.Data;

public static class SqlTarget
{
    public static string Validate(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("Cần cấu hình ConnectionStrings__Store trước khi chạy backend.");

        SqlConnectionStringBuilder target;
        try { target = new(connectionString); }
        catch (ArgumentException)
        {
            throw new InvalidOperationException("Cấu hình kết nối SQL Server không hợp lệ.");
        }

        if (string.IsNullOrWhiteSpace(target.DataSource) || string.IsNullOrWhiteSpace(target.InitialCatalog) ||
            new[] { "master", "model", "msdb", "tempdb" }.Contains(target.InitialCatalog, StringComparer.OrdinalIgnoreCase))
            throw new InvalidOperationException("Cần máy chủ và CSDL ứng dụng riêng; không được dùng CSDL hệ thống.");

        return target.ConnectionString;
    }
}
