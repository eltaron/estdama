using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace TMS.Infrastructure.Services;

public interface IDatabaseBackupService
{
    string CreateBackup(string backupDirectory);
    void RestoreFromFile(string backupFilePath);
}

public class DatabaseBackupService : IDatabaseBackupService
{
    private readonly string _connectionString;
    private readonly string _database;

    public DatabaseBackupService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection not configured.");
        var builder = new SqlConnectionStringBuilder(_connectionString);
        _database = builder.InitialCatalog;
    }

    public string CreateBackup(string backupDirectory)
    {
        Directory.CreateDirectory(backupDirectory);
        var fileName = $"TMS_{DateTime.Now:yyyyMMdd_HHmmss}.bak";
        var fullPath = Path.Combine(backupDirectory, fileName);

        using var connection = new SqlConnection(_connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandTimeout = 300;
        command.CommandText =
            $"BACKUP DATABASE [{_database}] TO DISK = @path WITH INIT, NAME = N'TMS Backup';";
        command.Parameters.AddWithValue("@path", fullPath);
        command.ExecuteNonQuery();

        return fileName;
    }

    public void RestoreFromFile(string backupFilePath)
    {
        var builder = new SqlConnectionStringBuilder(_connectionString) { InitialCatalog = "master" };
        using var connection = new SqlConnection(builder.ConnectionString);
        connection.Open();

        try
        {
            using (var single = connection.CreateCommand())
            {
                single.CommandTimeout = 120;
                single.CommandText = $"ALTER DATABASE [{_database}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;";
                single.ExecuteNonQuery();
            }

            using (var restore = connection.CreateCommand())
            {
                restore.CommandTimeout = 600;
                restore.CommandText =
                    $"RESTORE DATABASE [{_database}] FROM DISK = @path WITH REPLACE, RECOVERY;";
                restore.Parameters.AddWithValue("@path", backupFilePath);
                restore.ExecuteNonQuery();
            }
        }
        finally
        {
            try
            {
                using var multi = connection.CreateCommand();
                multi.CommandTimeout = 60;
                multi.CommandText = $"ALTER DATABASE [{_database}] SET MULTI_USER;";
                multi.ExecuteNonQuery();
            }
            catch
            {
                // ignore - database may not be available right after restore
            }
        }
    }
}