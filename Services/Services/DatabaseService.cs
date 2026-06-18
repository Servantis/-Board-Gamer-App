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

    public string GetDatabasePath()
    {
        return DatabasePath;
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
        }
        finally
        {
            _initializationLock.Release();
        }
    }

    public async Task<SQLiteAsyncConnection> GetConnectionAsync()
    {
        await InitializeAsync();

        if (_database is null)
            throw new InvalidOperationException("Die Datenbank konnte nicht initialisiert werden.");

        return _database;
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

    public async Task<List<T>> GetAllAsync<T>() where T : new()
    {
        var database = await GetConnectionAsync();
        return await database.Table<T>().ToListAsync();
    }

    public async Task<List<T>> GetNotDeletedAsync<T>() where T : BaseSyncEntity, new()
    {
        var database = await GetConnectionAsync();

        return await database
            .Table<T>()
            .Where(x => x.DeletedAt == null)
            .ToListAsync();
    }

    public async Task<T?> GetByIdAsync<T>(string id) where T : BaseSyncEntity, new()
    {
        var database = await GetConnectionAsync();

        return await database
            .Table<T>()
            .Where(x => x.Id == id)
            .FirstOrDefaultAsync();
    }

    public async Task<int> InsertAsync<T>(T entity) where T : BaseSyncEntity
    {
        var database = await GetConnectionAsync();

        var now = DateTimeHelper.UtcNowIsoString();

        if (string.IsNullOrWhiteSpace(entity.Id))
            entity.Id = Guid.NewGuid().ToString();

        entity.CreatedAt = now;
        entity.UpdatedAt = now;
        entity.DeletedAt = null;
        entity.Version = 1;

        return await database.InsertAsync(entity);
    }

    public async Task<int> UpdateAsync<T>(T entity) where T : BaseSyncEntity
    {
        var database = await GetConnectionAsync();

        entity.UpdatedAt = DateTimeHelper.UtcNowIsoString();
        entity.Version += 1;

        return await database.UpdateAsync(entity);
    }

    public async Task<int> SoftDeleteAsync<T>(T entity) where T : BaseSyncEntity
    {
        var database = await GetConnectionAsync();

        var now = DateTimeHelper.UtcNowIsoString();

        entity.DeletedAt = now;
        entity.UpdatedAt = now;
        entity.Version += 1;

        return await database.UpdateAsync(entity);
    }

    public async Task<int> HardDeleteAsync<T>(T entity)
    {
        var database = await GetConnectionAsync();
        return await database.DeleteAsync(entity);
    }

    public async Task<SyncState> GetSyncStateAsync()
    {
        var database = await GetConnectionAsync();

        var syncState = await database
            .Table<SyncState>()
            .Where(x => x.Id == "default")
            .FirstOrDefaultAsync();

        if (syncState is not null)
            return syncState;

        syncState = new SyncState
        {
            Id = "default",
            LastPullAt = null,
            LastPushAt = null
        };

        await database.InsertAsync(syncState);

        return syncState;
    }

    public async Task UpdateSyncStateAsync(SyncState syncState)
    {
        var database = await GetConnectionAsync();
        await database.UpdateAsync(syncState);
    }

    public async Task<int> InsertSyncOutboxEntryAsync(SyncOutboxEntry entry)
    {
        var database = await GetConnectionAsync();

        if (string.IsNullOrWhiteSpace(entry.Id))
            entry.Id = Guid.NewGuid().ToString();

        if (string.IsNullOrWhiteSpace(entry.CreatedAt))
            entry.CreatedAt = DateTimeHelper.UtcNowIsoString();

        return await database.InsertAsync(entry);
    }

    public async Task<List<SyncOutboxEntry>> GetPendingSyncOutboxEntriesAsync(int limit = 50)
    {
        var database = await GetConnectionAsync();

        return await database
            .Table<SyncOutboxEntry>()
            .OrderBy(x => x.CreatedAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<int> DeleteSyncOutboxEntryAsync(string id)
    {
        var database = await GetConnectionAsync();

        var entry = await database
            .Table<SyncOutboxEntry>()
            .Where(x => x.Id == id)
            .FirstOrDefaultAsync();

        if (entry is null)
            return 0;

        return await database.DeleteAsync(entry);
    }

    public async Task MarkSyncOutboxEntryFailedAsync(string id, string errorMessage)
    {
        var database = await GetConnectionAsync();

        var entry = await database
            .Table<SyncOutboxEntry>()
            .Where(x => x.Id == id)
            .FirstOrDefaultAsync();

        if (entry is null)
            return;

        entry.RetryCount += 1;
        entry.LastError = errorMessage;

        await database.UpdateAsync(entry);
    }

    public async Task ResetDatabaseForDevelopmentAsync()
    {
        if (_database is not null)
        {
            await _database.CloseAsync();
            _database = null;
        }

        if (File.Exists(DatabasePath))
            File.Delete(DatabasePath);

        await InitializeAsync();
    }
}