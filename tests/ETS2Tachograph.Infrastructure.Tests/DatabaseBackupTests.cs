using Microsoft.Data.Sqlite;
using ETS2Tachograph.Infrastructure.Persistence;

namespace ETS2Tachograph.Infrastructure.Tests;

public sealed class DatabaseBackupTests
{
    [Fact]
    public void Existing_database_is_backed_up_before_migration()
    {
        using var temporary = new TemporaryDirectory();
        var databasePath = Path.Combine(temporary.Path, "tachograph.db");
        using (var connection = new SqliteConnection($"Data Source={databasePath};Pooling=False"))
        {
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "CREATE TABLE TestData (Value INTEGER NOT NULL); INSERT INTO TestData VALUES (327);";
            command.ExecuteNonQuery();
        }

        var backupPath = DatabaseBackup.CreateBeforeMigration(
            databasePath,
            new DateTimeOffset(2026, 7, 15, 2, 30, 0, TimeSpan.FromHours(2)));

        Assert.NotNull(backupPath);
        Assert.Equal($"{databasePath}.bak.20260715-023000-000", backupPath);
        Assert.True(File.Exists(backupPath));
        using var backup = new SqliteConnection($"Data Source={backupPath};Mode=ReadOnly;Pooling=False");
        backup.Open();
        using var read = backup.CreateCommand();
        read.CommandText = "SELECT Value FROM TestData";
        Assert.Equal(327L, (long)read.ExecuteScalar()!);
    }

    [Fact]
    public void Missing_database_does_not_create_a_backup()
    {
        using var temporary = new TemporaryDirectory();
        var databasePath = Path.Combine(temporary.Path, "tachograph.db");

        var backupPath = DatabaseBackup.CreateBeforeMigration(databasePath);

        Assert.Null(backupPath);
        Assert.Empty(Directory.GetFiles(temporary.Path));
    }

    [Fact]
    public void Repeated_backup_with_the_same_timestamp_never_overwrites_previous_copy()
    {
        using var temporary = new TemporaryDirectory();
        var databasePath = Path.Combine(temporary.Path, "tachograph.db");
        using (var connection = new SqliteConnection($"Data Source={databasePath};Pooling=False"))
        {
            connection.Open();
        }

        var timestamp = new DateTimeOffset(2026, 7, 15, 2, 30, 0, TimeSpan.FromHours(2));
        var first = DatabaseBackup.CreateBeforeMigration(databasePath, timestamp);
        var second = DatabaseBackup.CreateBeforeMigration(databasePath, timestamp);

        Assert.NotEqual(first, second);
        Assert.True(File.Exists(first));
        Assert.True(File.Exists(second));
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"ets2-tacho-{Guid.NewGuid():N}");
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose() => Directory.Delete(Path, recursive: true);
    }
}
