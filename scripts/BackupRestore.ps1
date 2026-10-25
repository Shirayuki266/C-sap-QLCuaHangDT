param(
    [ValidateSet('Backup','Verify','Restore')][string]$Mode = 'Backup',
    [string]$Server = 'localhost',
    [string]$Database = 'PhoneStore',
    [string]$BackupFile,
    [string]$RestoreDatabase = ('PhoneStore_Restored_' + (Get-Date -Format yyyyMMdd_HHmmss))
)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Data
$connectionBuilder = New-Object System.Data.SqlClient.SqlConnectionStringBuilder
$connectionBuilder['Data Source'] = $Server
$connectionBuilder['Initial Catalog'] = 'master'
$connectionBuilder['Integrated Security'] = $true
$connectionBuilder.Encrypt = $true
$connectionBuilder['TrustServerCertificate'] = $true
$connection = New-Object System.Data.SqlClient.SqlConnection $connectionBuilder.ConnectionString
function Scalar([string]$sql) { $command = $connection.CreateCommand(); $command.CommandText = $sql; try { $command.ExecuteScalar() } finally { $command.Dispose() } }
function Literal([string]$value) { "N'" + $value.Replace("'", "''") + "'" }
try {
    $connection.Open()
    if ($Mode -eq 'Backup') {
        if (!$BackupFile) { $backupDirectory = [string](Scalar "SELECT CAST(SERVERPROPERTY('InstanceDefaultBackupPath') AS nvarchar(1000))"); if (!$backupDirectory) { throw 'Cần chỉ định BackupFile mà tài khoản dịch vụ SQL Server có quyền ghi.' }; $BackupFile = Join-Path $backupDirectory ($Database + '_' + (Get-Date -Format yyyyMMdd_HHmmss) + '.bak') }
        $command = $connection.CreateCommand(); $command.CommandTimeout = 300
        $command.CommandText = 'BACKUP DATABASE @databaseName TO DISK=@file WITH COPY_ONLY,CHECKSUM; RESTORE VERIFYONLY FROM DISK=@file WITH CHECKSUM;'
        [void]$command.Parameters.AddWithValue('@databaseName', $Database); [void]$command.Parameters.AddWithValue('@file', $BackupFile)
        [void]$command.ExecuteNonQuery(); $command.Dispose(); Write-Output "Backup và VERIFYONLY thành công: $BackupFile"
    } elseif ($Mode -eq 'Verify') {
        if (!$BackupFile) { throw 'Cần BackupFile.' }
        $command=$connection.CreateCommand(); $command.CommandTimeout=300; $command.CommandText='RESTORE VERIFYONLY FROM DISK=@file WITH CHECKSUM;'; [void]$command.Parameters.AddWithValue('@file',$BackupFile); [void]$command.ExecuteNonQuery();$command.Dispose();Write-Output 'VERIFYONLY thành công.'
    } else {
        if (!$BackupFile) { throw 'Cần BackupFile.' }
        if ($RestoreDatabase -notmatch '^PhoneStore_Restored_[A-Za-z0-9_]+$') { throw 'Tên đích phải bắt đầu PhoneStore_Restored_ để tránh ghi đè dữ liệu ứng dụng.' }
        $exists=$connection.CreateCommand();$exists.CommandText='SELECT DB_ID(@name)';[void]$exists.Parameters.AddWithValue('@name',$RestoreDatabase);$existing=$exists.ExecuteScalar();$exists.Dispose();if($existing -isnot [DBNull]){throw 'CSDL đích đã tồn tại. Script không ghi đè hoặc xóa CSDL.'}
        $dataDirectory=[string](Scalar "SELECT CAST(SERVERPROPERTY('InstanceDefaultDataPath') AS nvarchar(1000))");$logDirectory=[string](Scalar "SELECT CAST(SERVERPROPERTY('InstanceDefaultLogPath') AS nvarchar(1000))")
        $files=$connection.CreateCommand();$files.CommandText='RESTORE FILELISTONLY FROM DISK=@file';[void]$files.Parameters.AddWithValue('@file',$BackupFile);$reader=$files.ExecuteReader();$moves=New-Object 'System.Collections.Generic.List[string]';$index=0
        while($reader.Read()) { $logical=[string]$reader['LogicalName'];$type=[string]$reader['Type'];$directory=if($type -eq 'L'){$logDirectory}else{$dataDirectory};$extension=if($type -eq 'L'){'.ldf'}elseif($index -eq 0){'.mdf'}else{'.ndf'};$destination=Join-Path $directory ($RestoreDatabase+'_'+$index+$extension);$moves.Add('MOVE '+(Literal $logical)+' TO '+(Literal $destination));$index++ }
        $reader.Close();$files.Dispose();$restore=$connection.CreateCommand();$restore.CommandTimeout=300;$restore.CommandText='RESTORE DATABASE ['+$RestoreDatabase+'] FROM DISK='+(Literal $BackupFile)+' WITH '+($moves -join ',')+',CHECKSUM;';[void]$restore.ExecuteNonQuery();$restore.Dispose();Write-Output "Khôi phục thành công vào CSDL mới: $RestoreDatabase"
    }
} finally { $connection.Dispose() }

