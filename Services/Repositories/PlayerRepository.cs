using BoardGamerApp.Models;
using BoardGamerApp.Services;

namespace BoardGamerApp.Repositories;

public class PlayerRepository : IPlayerRepository
{
    private readonly DatabaseService _databaseService;

    public PlayerRepository(DatabaseService databaseService)
    {
        _databaseService = databaseService;
    }

    public async Task<List<Player>> GetActivePlayersAsync()
    {
        var database = await _databaseService.GetConnectionAsync();

        const string sql = """
            SELECT *
            FROM players
            WHERE is_active = 1
              AND deleted_at IS NULL
            ORDER BY name;
            """;

        return await database.QueryAsync<Player>(sql);
    }

    public async Task<Player?> GetPlayerByInstallationIdAsync(string installationId)
    {
        var database = await _databaseService.GetConnectionAsync();

        const string sql = """
            SELECT p.*
            FROM players p
            INNER JOIN player_devices pd ON pd.player_id = p.id
            WHERE pd.installation_id = ?
              AND pd.is_active = 1
              AND p.is_active = 1
              AND p.deleted_at IS NULL
            LIMIT 1;
            """;

        var result = await database.QueryAsync<Player>(sql, installationId);

        return result.FirstOrDefault();
    }

    public async Task<Player?> GetByIdAsync(string playerId)
    {
        var database = await _databaseService.GetConnectionAsync();

        const string sql = """
        SELECT *
        FROM players
        WHERE id = ?
          AND is_active = 1
          AND deleted_at IS NULL
        LIMIT 1;
        """;

        var result = await database.QueryAsync<Player>(sql, playerId);

        return result.FirstOrDefault();
    }


    public async Task UpdatePlayerProfileAsync(string playerId, string name, string? email)
    {
        var database = await _databaseService.GetConnectionAsync();

        var now = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");

        const string sql = """
        UPDATE players
        SET name = ?,
            email = ?,
            updated_at = ?,
            version = version + 1
        WHERE id = ?
          AND deleted_at IS NULL;
        """;

        await database.ExecuteAsync(
            sql,
            name,
            string.IsNullOrWhiteSpace(email) ? null : email.Trim(),
            now,
            playerId);
    }

}