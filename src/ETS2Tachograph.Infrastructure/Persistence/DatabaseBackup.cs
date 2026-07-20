using Microsoft.Data.Sqlite;

namespace ETS2Tachograph.Infrastructure.Persistence;

public static class DatabaseBackup
{
    public static string? CreateBeforeMigration(
        string databasePath,
        DateTimeOffset? timestamp = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);

        var sourcePath = Path.GetFullPath(databasePath);
        if (!File.Exists(sourcePath))
        {
            return null;
        }

        var backupPath = UniqueBackupPath(sourcePath, timestamp ?? DateTimeOffset.Now);
        using var source = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = sourcePath,
            Mode = SqliteOpenMode.ReadWrite,
            Pooling = false
        }.ToString());
        using var destination = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = backupPath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Pooling = false
        }.ToString());

        source.Open();
        destination.Open();
        source.BackupDatabase(destination);
        return backupPath;
    }

    private static string UniqueBackupPath(string databasePath, DateTimeOffset timestamp)
    {
        var stem = $"{databasePath}.bak.{timestamp:yyyyMMdd-HHmmss-fff}";
        var candidate = stem;
        var suffix = 1;
        while (File.Exists(candidate))
        {
            candidate = $"{stem}-{suffix++}";
        }

        return candidate;
    }
}
