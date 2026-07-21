using BoardGamerApp.Models;
using BoardGamerApp.Services;

namespace BoardGamerApp.Repositories;

public class PlayerDeviceRepository : IPlayerDeviceRepository
{
    private readonly DatabaseService _databaseService;
    private readonly SyncOutboxService _syncOutboxService;

    public PlayerDeviceRepository(
        DatabaseService databaseService,
        SyncOutboxService syncOutboxService)
    {
        _databaseService = databaseService;
        _syncOutboxService = syncOutboxService;
    }

    public async Task LinkInstallationToPlayerAsync(
        string playerId,
        string installationId,
        string? deviceName,
        string? platform)
    {
        var database = await _databaseService.GetConnectionAsync();

        var now = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");

        const string checkSql = """
            SELECT COUNT(*)
            FROM player_devices
            WHERE installation_id = ?;
            """;

        var existingCount = await database.ExecuteScalarAsync<int>(
            checkSql,
            installationId);

        if (existingCount > 0)
        {
            const string updateSql = """
                UPDATE player_devices
                SET player_id = ?,
                    device_name = ?,
                    platform = ?,
                    is_active = 1,
                    updated_at = ?,
                    last_seen_at = ?,
                    version = version + 1
                WHERE installation_id = ?;
                """;

            await database.ExecuteAsync(
                updateSql,
                playerId,
                deviceName,
                platform,
                now,
                now,
                installationId);

            var existingDeviceId = await GetDeviceIdByInstallationIdAsync(
                database,
                installationId);

            if (!string.IsNullOrWhiteSpace(existingDeviceId))
            {
                await _syncOutboxService.AddFromDatabaseAsync(
                    database,
                    "player_devices",
                    existingDeviceId,
                    BoardGamerConstants.SyncOperations.Update);
            }

            return;
        }

        const string insertSql = """
            INSERT INTO player_devices (
                id,
                player_id,
                installation_id,
                device_name,
                platform,
                is_active,
                created_at,
                updated_at,
                last_seen_at,
                version
            )
            VALUES (?, ?, ?, ?, ?, 1, ?, ?, ?, 1);
            """;

        var deviceId = Guid.NewGuid().ToString();

        await database.ExecuteAsync(
            insertSql,
            deviceId,
            playerId,
            installationId,
            deviceName,
            platform,
            now,
            now,
            now);

        await _syncOutboxService.AddFromDatabaseAsync(
            database,
            "player_devices",
            deviceId,
            BoardGamerConstants.SyncOperations.Insert);
    }

    public async Task UpdateLastSeenAsync(string installationId)
    {
        var database = await _databaseService.GetConnectionAsync();

        var now = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");

        const string sql = """
            UPDATE player_devices
            SET last_seen_at = ?,
                updated_at = ?,
                version = version + 1
            WHERE installation_id = ?;
            """;

        await database.ExecuteAsync(sql, now, now, installationId);

        var deviceId = await GetDeviceIdByInstallationIdAsync(
            database,
            installationId);

        if (!string.IsNullOrWhiteSpace(deviceId))
        {
            await _syncOutboxService.AddFromDatabaseAsync(
                database,
                "player_devices",
                deviceId,
                BoardGamerConstants.SyncOperations.Update);
        }
    }

    public async Task UnlinkInstallationAsync(string installationId)
    {
        var database = await _databaseService.GetConnectionAsync();

        var now = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");

        const string sql = """
        UPDATE player_devices
        SET is_active = 0,
            updated_at = ?,
            deleted_at = ?,
            version = version + 1
        WHERE installation_id = ?
          AND deleted_at IS NULL;
        """;

        await database.ExecuteAsync(sql, now, now, installationId);

        var deviceId = await GetDeviceIdByInstallationIdAsync(
            database,
            installationId);

        if (!string.IsNullOrWhiteSpace(deviceId))
        {
            await _syncOutboxService.AddFromDatabaseAsync(
                database,
                "player_devices",
                deviceId,
                BoardGamerConstants.SyncOperations.Delete);
        }
    }

    private static async Task<string?> GetDeviceIdByInstallationIdAsync(
        SQLite.SQLiteAsyncConnection database,
        string installationId)
    {
        const string sql = """
        SELECT id
        FROM player_devices
        WHERE installation_id = ?
        LIMIT 1;
        """;

        return await database.ExecuteScalarAsync<string>(
            sql,
            installationId);
    }
}