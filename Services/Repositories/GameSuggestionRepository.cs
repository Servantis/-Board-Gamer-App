using BoardGamerApp.Models;
using BoardGamerApp.Services;

namespace BoardGamerApp.Repositories;

public class GameSuggestionRepository : IGameSuggestionRepository
{
    private readonly DatabaseService _databaseService;

    public GameSuggestionRepository(DatabaseService databaseService)
    {
        _databaseService = databaseService;
    }

    public async Task<List<GameSuggestionListItem>> GetSuggestionsForGameNightAsync(
        string gameNightId,
        string currentPlayerId)
    {
        var database = await _databaseService.GetConnectionAsync();

        const string sql = """
            SELECT
                gs.id AS SuggestionId,
                gs.game_night_id AS GameNightId,
                gs.game_id AS GameId,
                g.title AS GameTitle,
                g.game_genre AS GameGenre,
                g.min_players AS MinPlayers,
                g.max_players AS MaxPlayers,
                g.duration_minutes AS DurationMinutes,
                gs.suggested_by_player_id AS SuggestedByPlayerId,
                p.name AS SuggestedByPlayerName,
                gs.comment AS Comment,
                COUNT(gv.id) AS VoteCount,
                CASE 
                    WHEN SUM(
                        CASE 
                            WHEN gv.player_id = ? 
                             AND gv.deleted_at IS NULL 
                            THEN 1 
                            ELSE 0 
                        END
                    ) > 0 
                    THEN 1 
                    ELSE 0 
                END AS HasCurrentPlayerVotedValue
            FROM game_suggestions gs
            INNER JOIN games g ON g.id = gs.game_id
            INNER JOIN players p ON p.id = gs.suggested_by_player_id
            LEFT JOIN game_votes gv 
                ON gv.suggestion_id = gs.id
               AND gv.deleted_at IS NULL
            WHERE gs.game_night_id = ?
              AND gs.deleted_at IS NULL
              AND g.deleted_at IS NULL
              AND p.deleted_at IS NULL
            GROUP BY
                gs.id,
                gs.game_night_id,
                gs.game_id,
                g.title,
                g.game_genre,
                g.min_players,
                g.max_players,
                g.duration_minutes,
                gs.suggested_by_player_id,
                p.name,
                gs.comment
            ORDER BY VoteCount DESC, g.title;
            """;

        return await database.QueryAsync<GameSuggestionListItem>(
            sql,
            currentPlayerId,
            gameNightId);
    }

    public async Task AddSuggestionAsync(
        string gameNightId,
        string gameId,
        string suggestedByPlayerId,
        string? comment)
    {
        var database = await _databaseService.GetConnectionAsync();

        var now = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");

        // Wegen UNIQUE(game_night_id, game_id):
        // Wenn der Vorschlag früher soft-deleted wurde, reaktivieren wir ihn.
        const string existingSql = """
            SELECT id
            FROM game_suggestions
            WHERE game_night_id = ?
              AND game_id = ?
            LIMIT 1;
            """;

        var existing = await database.QueryScalarsAsync<string>(
            existingSql,
            gameNightId,
            gameId);

        var existingId = existing.FirstOrDefault();

        if (!string.IsNullOrWhiteSpace(existingId))
        {
            const string reactivateSql = """
                UPDATE game_suggestions
                SET suggested_by_player_id = ?,
                    comment = ?,
                    deleted_at = NULL,
                    updated_at = ?,
                    version = version + 1
                WHERE id = ?;
                """;

            await database.ExecuteAsync(
                reactivateSql,
                suggestedByPlayerId,
                string.IsNullOrWhiteSpace(comment) ? null : comment.Trim(),
                now,
                existingId);

            return;
        }

        const string insertSql = """
            INSERT INTO game_suggestions (
                id,
                game_night_id,
                game_id,
                suggested_by_player_id,
                comment,
                created_at,
                updated_at,
                version
            )
            VALUES (?, ?, ?, ?, ?, ?, ?, 1);
            """;

        await database.ExecuteAsync(
            insertSql,
            Guid.NewGuid().ToString(),
            gameNightId,
            gameId,
            suggestedByPlayerId,
            string.IsNullOrWhiteSpace(comment) ? null : comment.Trim(),
            now,
            now);
    }

    public async Task ToggleVoteAsync(
    string suggestionId,
    string playerId)
    {
        var database = await _databaseService.GetConnectionAsync();

        var now = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");

        const string existingSql = """
        SELECT 
            id AS Id,
            deleted_at AS DeletedAt
        FROM game_votes
        WHERE suggestion_id = ?
          AND player_id = ?
        LIMIT 1;
        """;

        var existingVotes = await database.QueryAsync<GameVoteExistingRow>(
            existingSql,
            suggestionId,
            playerId);

        var existingVote = existingVotes.FirstOrDefault();

        // Fall 1: Es gibt noch gar keinen Vote
        if (existingVote is null)
        {
            const string insertSql = """
            INSERT INTO game_votes (
                id,
                suggestion_id,
                player_id,
                vote_value,
                created_at,
                updated_at,
                version
            )
            VALUES (?, ?, ?, 1, ?, ?, 1);
            """;

            await database.ExecuteAsync(
                insertSql,
                Guid.NewGuid().ToString(),
                suggestionId,
                playerId,
                now,
                now);

            return;
        }

        // Fall 2: Vote existiert, wurde aber vorher entfernt → reaktivieren
        if (!string.IsNullOrWhiteSpace(existingVote.DeletedAt))
        {
            const string reactivateSql = """
            UPDATE game_votes
            SET vote_value = 1,
                deleted_at = NULL,
                updated_at = ?,
                version = version + 1
            WHERE id = ?;
            """;

            await database.ExecuteAsync(
                reactivateSql,
                now,
                existingVote.Id);

            return;
        }

        // Fall 3: Vote existiert und ist aktiv → entfernen
        const string softDeleteSql = """
        UPDATE game_votes
        SET deleted_at = ?,
            updated_at = ?,
            version = version + 1
        WHERE id = ?;
        """;

        await database.ExecuteAsync(
            softDeleteSql,
            now,
            now,
            existingVote.Id);
    }

    public async Task SoftDeleteSuggestionAsync(string suggestionId)
    {
        var database = await _databaseService.GetConnectionAsync();

        var now = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");

        const string sql = """
            UPDATE game_suggestions
            SET deleted_at = ?,
                updated_at = ?,
                version = version + 1
            WHERE id = ?
              AND deleted_at IS NULL;
            """;

        await database.ExecuteAsync(
            sql,
            now,
            now,
            suggestionId);
    }

    private class GameVoteExistingRow
    {
        public string Id { get; set; } = string.Empty;

        public string? DeletedAt { get; set; }
    }
}