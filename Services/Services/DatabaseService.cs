using BoardGamerApp.Models;
using SQLite;

namespace BoardGamerApp.Services;

public class DatabaseService
{
    private const string DatabaseFileName = "boardgamer.db";

    private SQLiteAsyncConnection? _database;
    private readonly SemaphoreSlim _initializationLock = new(1, 1);

    private string DatabasePath =>
        Path.Combine(FileSystem.AppDataDirectory, DatabaseFileName);

    public async Task<SQLiteAsyncConnection> GetDatabaseAsync()
    {
        await InitializeAsync();

        if (_database is null)
            throw new InvalidOperationException("Die Datenbank konnte nicht initialisiert werden.");

        return _database;
    }

    public async Task InitializeAsync()
    {
        if (_database is not null)
            return;

        await _initializationLock.WaitAsync();

        try
        {
            if (_database is not null)
                return;

            await CopyDatabaseIfNotExistsAsync();

            _database = new SQLiteAsyncConnection(
                DatabasePath,
                SQLiteOpenFlags.ReadWrite |
                SQLiteOpenFlags.Create |
                SQLiteOpenFlags.SharedCache
            );

            await _database.ExecuteAsync("PRAGMA foreign_keys = ON;");

            await EnsureDefaultSyncStateAsync();

            // Optional:
            // Nur aktiv lassen, wenn die App fehlende Tabellen selbst nachziehen soll.
            // await CreateTablesAsync();
        }
        finally
        {
            _initializationLock.Release();
        }
    }

    private async Task CopyDatabaseIfNotExistsAsync()
    {
        if (File.Exists(DatabasePath))
            return;

        using Stream inputStream = await FileSystem.OpenAppPackageFileAsync(DatabaseFileName);

        using FileStream outputStream = File.Create(DatabasePath);

        await inputStream.CopyToAsync(outputStream);
    }

    private async Task EnsureDefaultSyncStateAsync()
    {
        if (_database is null)
            throw new InvalidOperationException("Datenbank ist nicht initialisiert.");

        var existingSyncState = await _database
            .Table<SyncState>()
            .Where(x => x.Id == "default")
            .FirstOrDefaultAsync();

        if (existingSyncState is not null)
            return;

        var syncState = new SyncState
        {
            Id = "default",
            LastPullAt = null,
            LastPushAt = null
        };

        await _database.InsertAsync(syncState);
    }
}