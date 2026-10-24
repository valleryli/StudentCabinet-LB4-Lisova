using SQLite;
using StudentCabinet_LB1.Models;

namespace StudentCabinet_LB1.Data
{
    public static class DatabaseConstants
    {
        public const string FileName = "studentcabinet.db3";
        public const int SchemaVersion = 1;

        public static string FullPath => Path.Combine(FileSystem.AppDataDirectory, FileName);
    }

    // Єдине підключення до SQLite для всього застосунку (singleton)
    public class AppDatabase
    {
        private SQLiteAsyncConnection? _connection;
        private readonly SemaphoreSlim _initLock = new(1, 1);

        public async Task<SQLiteAsyncConnection> GetConnectionAsync()
        {
            if (_connection is not null) return _connection;

            await _initLock.WaitAsync();
            try
            {
                if (_connection is not null) return _connection;

                var connection = new SQLiteAsyncConnection(
                    DatabaseConstants.FullPath,
                    SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache);

                await connection.CreateTableAsync<Student>();
                await connection.CreateTableAsync<SyncQueueItem>();
                await MigrateAsync(connection);

                _connection = connection;
                return _connection;
            }
            finally
            {
                _initLock.Release();
            }
        }

        // Версіонування схеми через PRAGMA user_version
        private static async Task MigrateAsync(SQLiteAsyncConnection db)
        {
            var version = await db.ExecuteScalarAsync<int>("PRAGMA user_version");

            // Приклад майбутньої міграції:
            // if (version < 2) await db.ExecuteAsync("ALTER TABLE Student ADD COLUMN Email TEXT");

            if (version < DatabaseConstants.SchemaVersion)
                await db.ExecuteAsync($"PRAGMA user_version = {DatabaseConstants.SchemaVersion}");
        }
    }
}
