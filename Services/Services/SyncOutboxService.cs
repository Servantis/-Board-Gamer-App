using System.Text.Json;
using System.Reflection;
using BoardGamerApp.Models;
using SQLite;
using Microsoft.Data.Sqlite;

namespace BoardGamerApp.Services;

/// <summary>
/// Zentrale Stelle für alle Einträge in die lokale sync_outbox.
/// Repositories sollen Änderungen nur noch lokal speichern und danach diesen Service aufrufen.
/// Dadurch wird verhindert, dass einzelne Add/Update/Delete-Methoden den Sync vergessen.
/// </summary>
public class SyncOutboxService
{
    private readonly DatabaseService _databaseService;

    private static readonly HashSet<string> AllowedEntityTables = new(StringComparer.OrdinalIgnoreCase)
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

    private static readonly HashSet<string> AllowedOperations = new(StringComparer.OrdinalIgnoreCase)
    {
        BoardGamerConstants.SyncOperations.Insert,
        BoardGamerConstants.SyncOperations.Update,
        BoardGamerConstants.SyncOperations.Delete
    };

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false
    };

    public SyncOutboxService(DatabaseService databaseService)
    {
        _databaseService = databaseService;
    }

    public async Task AddEntityAsync<T>(
        SQLiteAsyncConnection database,
        string entityName,
        T entity,
        string operation)
        where T : BaseSyncEntity
    {
        Validate(entityName, entity.Id, operation);

        var payload = BuildPayloadFromEntity(entity);

        await AddPayloadAsync(
            database,
            entityName,
            entity.Id,
            operation,
            payload);
    }

    public async Task AddFromDatabaseAsync(
        SQLiteAsyncConnection database,
        string entityName,
        string entityId,
        string operation)
    {
        Validate(entityName, entityId, operation);

        var payload = await ReadPayloadFromDatabaseAsync(entityName, entityId);

        if (payload is null)
        {
            throw new InvalidOperationException(
                $"Datensatz für Sync-Outbox nicht gefunden: {entityName}/{entityId}");
        }

        await AddPayloadAsync(
            database,
            entityName,
            entityId,
            operation,
            payload);
    }

    public async Task AddPayloadAsync(
        SQLiteAsyncConnection database,
        string entityName,
        string entityId,
        string operation,
        Dictionary<string, object?> payload)
    {
        Validate(entityName, entityId, operation);

        payload["id"] = entityId;

        var outboxEntry = new SyncOutboxEntry
        {
            Id = Guid.NewGuid().ToString(),
            EntityName = entityName,
            EntityId = entityId,
            Operation = operation.ToUpperInvariant(),
            PayloadJson = JsonSerializer.Serialize(payload, JsonOptions),
            CreatedAt = DateTimeHelper.UtcNowIsoString(),
            RetryCount = 0,
            LastError = null
        };

        await database.InsertAsync(outboxEntry);
    }

    private async Task<Dictionary<string, object?>?> ReadPayloadFromDatabaseAsync(
        string tableName,
        string entityId)
    {
        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = _databaseService.GetDatabasePath()
        }.ToString();

        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();

        command.CommandText = $"""
            SELECT *
            FROM {tableName}
            WHERE id = @id
            LIMIT 1;
            """;

        command.Parameters.AddWithValue("@id", entityId);

        await using var reader = await command.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
            return null;

        var payload = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < reader.FieldCount; i++)
        {
            var columnName = reader.GetName(i);

            object? value = reader.IsDBNull(i)
                ? null
                : reader.GetValue(i);

            payload[columnName] = value;
        }

        return payload;
    }

    private static Dictionary<string, object?> BuildPayloadFromEntity<T>(T entity)
        where T : BaseSyncEntity
    {
        var payload = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        var properties = typeof(T)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(property =>
                property.CanRead &&
                property.GetCustomAttribute<IgnoreAttribute>() is null);

        foreach (var property in properties)
        {
            var columnAttribute = property.GetCustomAttribute<ColumnAttribute>();

            var columnName = columnAttribute?.Name;

            if (string.IsNullOrWhiteSpace(columnName))
                continue;

            payload[columnName] = property.GetValue(entity);
        }

        return payload;
    }

    private static void Validate(
        string entityName,
        string entityId,
        string operation)
    {
        if (string.IsNullOrWhiteSpace(entityName))
            throw new InvalidOperationException("EntityName darf nicht leer sein.");

        if (!AllowedEntityTables.Contains(entityName))
            throw new InvalidOperationException($"Tabelle ist nicht für Sync erlaubt: {entityName}");

        if (string.IsNullOrWhiteSpace(entityId))
            throw new InvalidOperationException("EntityId darf nicht leer sein.");

        if (string.IsNullOrWhiteSpace(operation))
            throw new InvalidOperationException("Operation darf nicht leer sein.");

        if (!AllowedOperations.Contains(operation))
            throw new InvalidOperationException($"Operation ist nicht erlaubt: {operation}");
    }
}
