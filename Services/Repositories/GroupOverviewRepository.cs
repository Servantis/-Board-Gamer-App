using BoardGamerApp.Models;
using BoardGamerApp.Services;
using SQLite;

namespace BoardGamerApp.Repositories;

public class GroupOverviewRepository
{
    private readonly DatabaseService _databaseService;
    private readonly SyncOutboxService _syncOutboxService;

    public GroupOverviewRepository(
        DatabaseService databaseService,
        SyncOutboxService syncOutboxService)
    {
        _databaseService = databaseService;
        _syncOutboxService = syncOutboxService;
    }

    // Liefert alle aktiven Gruppen.
    public async Task<List<GamingGroup>> GetAllGroupsAsync()
    {
        var database = await _databaseService.GetConnectionAsync();

        return await database
            .Table<GamingGroup>()
            .Where(group => group.DeletedAt == null)
            .OrderBy(group => group.Name)
            .ToListAsync();
    }

    // Liefert alle Gruppen eines bestimmten Spielers.
    public async Task<List<GamingGroupListItem>> GetGroupsByPlayerIdAsync(
        string playerId)
    {
        var database = await _databaseService.GetConnectionAsync();


        const string sql = """
            SELECT
                gg.id AS Id,
                gg.name AS Name,
                gg.description AS Description,
                gg.created_by_player_id AS CreatedByPlayerId,
                p.name AS CreatedByPlayerName
            FROM gaming_groups gg
            INNER JOIN group_members gm
                ON gm.group_id = gg.id
            INNER JOIN players p
                ON p.id = gg.created_by_player_id
            WHERE gm.player_id = ?
              AND gm.status = 'active'
              AND gm.deleted_at IS NULL
              AND gg.deleted_at IS NULL
            ORDER BY gg.name;
            """;

        return await database.QueryAsync<GamingGroupListItem>(
            sql,
            playerId);
    }

    // Liefert eine Gruppe anhand ihrer Id.
    public async Task<GamingGroup?> GetGroupAsync(string groupId)
    {
        var database = await _databaseService.GetConnectionAsync();

        return await database.Table<GamingGroup>()
            .Where(group => group.Id == groupId &&
                            group.DeletedAt == null)
            .FirstOrDefaultAsync();
    }

    // Neue Gruppe speichern.
    public async Task AddGroupAsync(GamingGroup group)
    {
        var database = await _databaseService.GetConnectionAsync();

        group.CreatedAt = DateTimeHelper.UtcNowIsoString(); ;
        group.UpdatedAt = DateTimeHelper.UtcNowIsoString();
        group.Version = 1;

        await database.InsertAsync(group);

        await _syncOutboxService.AddEntityAsync(
            database,
            "gaming_groups",
            group,
            BoardGamerConstants.SyncOperations.Insert);
    }


    // Gruppe aktualisieren.
    public async Task UpdateGroupAsync(GamingGroup group)
    {
        var database = await _databaseService.GetConnectionAsync();

        group.UpdatedAt = DateTimeHelper.UtcNowIsoString();
        group.Version++;

        await database.UpdateAsync(group);

        await _syncOutboxService.AddEntityAsync(
            database,
            "gaming_groups",
            group,
            BoardGamerConstants.SyncOperations.Update);
    }

    // Soft Delete einer Gruppe.
    public async Task DeleteGroupAsync(string groupId)
    {
        var database = await _databaseService.GetConnectionAsync();

        var group = await GetGroupAsync(groupId);

        if (group == null)
            return;

        group.DeletedAt = DateTimeHelper.UtcNowIsoString();
        group.UpdatedAt = DateTimeHelper.UtcNowIsoString();
        group.Version++;

        await database.UpdateAsync(group);

        await _syncOutboxService.AddEntityAsync(
            database,
            "gaming_groups",
            group,
            BoardGamerConstants.SyncOperations.Delete);
    }

    // Gruppe verlassen, wenn Mitglied, aber kein Gruppenersteller
    public async Task LeaveGroupAsync(string groupId, string playerId)
    {
        var database = await _databaseService.GetConnectionAsync();

        const string sql = """
        UPDATE group_members
        SET deleted_at = ?,
            updated_at = ?,
            version = version + 1
        WHERE group_id = ?
          AND player_id = ?
          AND deleted_at IS NULL;
        """;

        var now = DateTimeHelper.UtcNowIsoString();

        await database.ExecuteAsync(
            sql,
            now,
            now,
            groupId,
            playerId);

        const string memberIdSql = """
        SELECT id
        FROM group_members
        WHERE group_id = ?
          AND player_id = ?
        LIMIT 1;
        """;

        var memberId = await database.ExecuteScalarAsync<string>(
            memberIdSql,
            groupId,
            playerId);

        if (!string.IsNullOrWhiteSpace(memberId))
        {
            await _syncOutboxService.AddFromDatabaseAsync(
                database,
                "group_members",
                memberId,
                BoardGamerConstants.SyncOperations.Delete);
        }
    }

    // Prüft, ob bereits eine Gruppe mit gleichem Namen existiert.
    public async Task<bool> ExistsAsync(string groupName)
    {
        var database = await _databaseService.GetConnectionAsync();

        return await database.Table<GamingGroup>()
            .Where(group => group.Name == groupName &&
                            group.DeletedAt == null)
            .CountAsync() > 0;
    }
}