using BoardGamerApp.Models;
using Microsoft.Data.Sqlite;
using SQLite;
using System.Text.Json;

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

    private static readonly HashSet<string> SyncAllowedTables = new(StringComparer.OrdinalIgnoreCase)
{
    "players",
    "player_devices",
    "gaming_groups",
    "group_members",
    "locations",
    "games",
    "game_nights",
    "attendance",
    "game_suggestions",
    "game_votes",
    "game_night_reviews"
};


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

    public async Task<List<Dictionary<string, object?>>> GetRowsForSyncAsync(string tableName)
    {
        await InitializeAsync();

        var allowedTables = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "players",
        "player_devices",
        "gaming_groups",
        "group_members",
        "locations",
        "games",
        "game_nights",
        "attendance",
        "game_suggestions",
        "game_votes",
        "game_night_reviews"
    };

        if (!allowedTables.Contains(tableName))
            throw new InvalidOperationException($"Tabelle ist nicht für Sync erlaubt: {tableName}");

        var rows = new List<Dictionary<string, object?>>();

        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = GetDatabasePath()
        }.ToString();

        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();

        command.CommandText = $"""
        SELECT *
        FROM {tableName}
        WHERE deleted_at IS NULL
        ORDER BY created_at ASC;
        """;

        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var row = new Dictionary<string, object?>();

            for (int i = 0; i < reader.FieldCount; i++)
            {
                var columnName = reader.GetName(i);

                object? value = reader.IsDBNull(i)
                    ? null
                    : reader.GetValue(i);

                row[columnName] = value;
            }

            rows.Add(row);
        }

        return rows;
    }

    public async Task ApplyRemoteChangeAsync(
    string tableName,
    string entityId,
    string operation,
    string payloadJson)
    {
        if (!SyncAllowedTables.Contains(tableName))
            throw new InvalidOperationException($"Tabelle ist nicht für Remote-Sync erlaubt: {tableName}");

        if (string.IsNullOrWhiteSpace(entityId))
            throw new InvalidOperationException("Remote-Änderung enthält keine Entity-ID.");

        if (string.IsNullOrWhiteSpace(operation))
            throw new InvalidOperationException("Remote-Änderung enthält keine Operation.");

        if (string.IsNullOrWhiteSpace(payloadJson))
            throw new InvalidOperationException("Remote-Änderung enthält keinen Payload.");

        await InitializeAsync();

        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = GetDatabasePath()
        }.ToString();

        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync();

        await using var pragmaCommand = connection.CreateCommand();
        pragmaCommand.CommandText = "PRAGMA foreign_keys = ON;";
        await pragmaCommand.ExecuteNonQueryAsync();

        var payload = DeserializePayload(payloadJson);
        payload["id"] = entityId;

        if (operation.Equals("DELETE", StringComparison.OrdinalIgnoreCase))
        {
            await ApplyRemoteDeleteAsync(connection, tableName, entityId, payload);
            return;
        }

        if (operation.Equals("INSERT", StringComparison.OrdinalIgnoreCase) ||
            operation.Equals("UPDATE", StringComparison.OrdinalIgnoreCase))
        {
            await ApplyRemoteUpsertAsync(connection, tableName, payload);
            return;
        }

        throw new InvalidOperationException($"Unbekannte Remote-Operation: {operation}");
    }


    private static async Task ApplyRemoteUpsertAsync(
    SqliteConnection connection,
    string tableName,
    Dictionary<string, object?> payload)
    {
        var tableColumns = await GetTableColumnsAsync(connection, tableName);

        var filteredPayload = payload
            .Where(x => tableColumns.Contains(x.Key))
            .ToDictionary(x => x.Key, x => x.Value);

        if (!filteredPayload.ContainsKey("id"))
            throw new InvalidOperationException("Payload enthält keine ID.");

        if (!filteredPayload.ContainsKey("updated_at"))
            filteredPayload["updated_at"] = DateTimeHelper.UtcNowIsoString();

        var columns = filteredPayload.Keys.ToList();

        var columnList = string.Join(", ", columns);
        var parameterList = string.Join(", ", columns.Select(column => $"@{column}"));

        var updateColumns = columns
            .Where(column => !column.Equals("id", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var updateStatement = updateColumns.Count == 0
            ? "id = excluded.id"
            : string.Join(", ", updateColumns.Select(column => $"{column} = excluded.{column}"));

        await using var command = connection.CreateCommand();

        command.CommandText = $"""
        INSERT INTO {tableName} ({columnList})
        VALUES ({parameterList})
        ON CONFLICT(id) DO UPDATE SET
            {updateStatement}
        WHERE excluded.updated_at >= {tableName}.updated_at
           OR {tableName}.updated_at IS NULL;
        """;

        foreach (var column in columns)
        {
            command.Parameters.AddWithValue(
                $"@{column}",
                filteredPayload[column] ?? DBNull.Value
            );
        }

        await command.ExecuteNonQueryAsync();
    }


    private static async Task ApplyRemoteDeleteAsync(
    SqliteConnection connection,
    string tableName,
    string entityId,
    Dictionary<string, object?> payload)
    {
        var existing = await RemoteRowExistsAsync(connection, tableName, entityId);

        if (!existing)
        {
            // Wenn der Datensatz lokal gar nicht existiert, ignorieren wir den Delete.
            // Das ist für Pull-Sync okay.
            return;
        }

        var deletedAt = payload.TryGetValue("deleted_at", out var deletedAtValue)
            ? deletedAtValue?.ToString()
            : null;

        if (string.IsNullOrWhiteSpace(deletedAt))
            deletedAt = DateTimeHelper.UtcNowIsoString();

        var updatedAt = payload.TryGetValue("updated_at", out var updatedAtValue)
            ? updatedAtValue?.ToString()
            : deletedAt;

        var version = payload.TryGetValue("version", out var versionValue)
            ? versionValue
            : null;

        await using var command = connection.CreateCommand();

        command.CommandText = $"""
        UPDATE {tableName}
        SET deleted_at = @deleted_at,
            updated_at = @updated_at,
            version = COALESCE(@version, version + 1)
        WHERE id = @id
          AND (
                @updated_at >= updated_at
                OR updated_at IS NULL
              );
        """;

        command.Parameters.AddWithValue("@id", entityId);
        command.Parameters.AddWithValue("@deleted_at", deletedAt);
        command.Parameters.AddWithValue("@updated_at", updatedAt ?? DateTimeHelper.UtcNowIsoString());
        command.Parameters.AddWithValue("@version", version ?? DBNull.Value);

        await command.ExecuteNonQueryAsync();
    }

    private static async Task<HashSet<string>> GetTableColumnsAsync(
    SqliteConnection connection,
    string tableName)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = $"PRAGMA table_info({tableName});";

        var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var columnName = reader.GetString(1);
            columns.Add(columnName);
        }

        return columns;
    }

    private static async Task<bool> RemoteRowExistsAsync(
    SqliteConnection connection,
    string tableName,
    string entityId)
    {
        await using var command = connection.CreateCommand();

        command.CommandText = $"""
        SELECT COUNT(*)
        FROM {tableName}
        WHERE id = @id;
        """;

        command.Parameters.AddWithValue("@id", entityId);

        var result = await command.ExecuteScalarAsync();

        return Convert.ToInt32(result) > 0;
    }


    private static Dictionary<string, object?> DeserializePayload(string payloadJson)
    {
        using var document = JsonDocument.Parse(payloadJson);

        if (document.RootElement.ValueKind != JsonValueKind.Object)
            throw new InvalidOperationException("Payload ist kein JSON-Objekt.");

        var result = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        foreach (var property in document.RootElement.EnumerateObject())
        {
            result[property.Name] = ConvertJsonElement(property.Value);
        }

        return result;
    }

    private static object? ConvertJsonElement(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),

            JsonValueKind.Number when element.TryGetInt64(out var longValue)
                => longValue,

            JsonValueKind.Number when element.TryGetDouble(out var doubleValue)
                => doubleValue,

            JsonValueKind.True => 1,

            JsonValueKind.False => 0,

            JsonValueKind.Null => null,

            JsonValueKind.Undefined => null,

            _ => element.GetRawText()
        };
    }

}