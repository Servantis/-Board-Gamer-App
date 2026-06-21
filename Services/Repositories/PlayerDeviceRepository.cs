using BoardGamerApp.Services;

namespace BoardGamerApp.Repositories;

public class PlayerDeviceRepository : IPlayerDeviceRepository
{
    private readonly DatabaseService _databaseService;

    public PlayerDeviceRepository(DatabaseService databaseService)
    {
        _databaseService = databaseService;
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

        await database.ExecuteAsync(
            insertSql,
            Guid.NewGuid().ToString(),
            playerId,
            installationId,
            deviceName,
            platform,
            now,
            now,
            now);
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
    }
}