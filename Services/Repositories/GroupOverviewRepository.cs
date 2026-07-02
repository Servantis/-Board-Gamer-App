using BoardGamerApp.Models;
using SQLite;

namespace BoardGamerApp.Services.Repositories;

public class GroupOverviewRepository
{
    private readonly DatabaseService _databaseService;

    public GroupOverviewRepository(DatabaseService databaseService)
    {
        _databaseService = databaseService;
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
    public async Task<List<GamingGroup>> GetGroupsByPlayerIdAsync(string playerId)
    {
        var database = await _databaseService.GetConnectionAsync();

        const string sql = @"
            SELECT gg.*
            FROM gaming_groups gg
            INNER JOIN group_members gm
                ON gm.group_id = gg.id
            WHERE gm.player_id = ?
            AND gm.deleted_at IS NULL
            AND gg.deleted_at IS NULL
            ORDER BY gg.name;";

        return await database.QueryAsync<GamingGroup>(sql, playerId);
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

        //group.CreatedAt = DateTime.UtcNow;
        //group.UpdatedAt = DateTime.UtcNow;
        group.Version = 1;

        await database.InsertAsync(group);
    }


    // Gruppe aktualisieren.
    public async Task UpdateGroupAsync(GamingGroup group)
    {
        var database = await _databaseService.GetConnectionAsync();

       // group.UpdatedAt = DateTime.UtcNow;
        group.Version++;

        await database.UpdateAsync(group);
    }

    // Soft Delete einer Gruppe.
    public async Task DeleteGroupAsync(string groupId)
    {
        var database = await _databaseService.GetConnectionAsync();

        var group = await GetGroupAsync(groupId);

        if (group == null)
            return;

       // group.DeletedAt = DateTime.UtcNow;
       // group.UpdatedAt = DateTime.UtcNow;
        group.Version++;

        await database.UpdateAsync(group);
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