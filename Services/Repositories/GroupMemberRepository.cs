using BoardGamerApp.Models;
using BoardGamerApp.Services;

namespace BoardGamerApp.Repositories;

public class GroupMemberRepository : IGroupMemberRepository
{
    private readonly DatabaseService _databaseService;

    public GroupMemberRepository(DatabaseService databaseService)
    {
        _databaseService = databaseService;
    }

    public async Task<GamingGroup?> GetDefaultGroupAsync()
    {
        var database = await _databaseService.GetConnectionAsync();

        const string sql = """
            SELECT *
            FROM gaming_groups
            WHERE deleted_at IS NULL
            ORDER BY created_at
            LIMIT 1;
            """;

        var result = await database.QueryAsync<GamingGroup>(sql);

        return result.FirstOrDefault();
    }

    public async Task<GamingGroup?> GetGroupByIdAsync(string groupId)
    {
        var database = await _databaseService.GetConnectionAsync();

        const string sql = """
        SELECT *
        FROM gaming_groups
        WHERE id = ?
          AND deleted_at IS NULL
        LIMIT 1;
        """;

        var result = await database.QueryAsync<GamingGroup>(sql, groupId);

        return result.FirstOrDefault();
    }

    public async Task<List<GroupMemberListItem>> GetMembersAsync()
    {
        var group = await GetDefaultGroupAsync();

        if (group is null)
        {
            return new List<GroupMemberListItem>();
        }

        return await GetMembersByGroupIdAsync(group.Id);
    }

    public async Task<List<GroupMemberListItem>> GetMembersByGroupIdAsync(string groupId)
    {
        var database = await _databaseService.GetConnectionAsync();

        const string sql = """
            SELECT
                gm.id AS Id,
                gm.group_id AS GroupId,
                gm.player_id AS PlayerId,
                p.name AS PlayerName,
                p.email AS PlayerEmail,
                gm.role AS Role,
                gm.status AS Status,
                gm.rotation_order AS RotationOrder,
                gg.name AS GroupName
            FROM group_members gm
            INNER JOIN players p ON p.id = gm.player_id
            INNER JOIN gaming_groups gg ON gg.id = gm.group_id
            WHERE gm.group_id = ?
              AND gm.deleted_at IS NULL
              AND p.deleted_at IS NULL
              AND gg.deleted_at IS NULL
            ORDER BY
                CASE 
                    WHEN gm.rotation_order IS NULL THEN 999999
                    ELSE gm.rotation_order
                END,
                p.name;
            """;

        return await database.QueryAsync<GroupMemberListItem>(sql, groupId);
    }

    public async Task<GroupMember?> GetMemberByIdAsync(string memberId)
    {
        var database = await _databaseService.GetConnectionAsync();

        const string sql = """
            SELECT *
            FROM group_members
            WHERE id = ?
              AND deleted_at IS NULL
            LIMIT 1;
            """;

        var result = await database.QueryAsync<GroupMember>(sql, memberId);

        return result.FirstOrDefault();
    }

    public async Task AddMemberAsync(
     string groupId,
     string playerId,
     string role = "member",
     int? rotationOrder = null)
    {
        var database = await _databaseService.GetConnectionAsync();

        var now = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");

        // Prüfen, ob bereits ein Datensatz für diese Gruppe und diesen Spieler existiert
        const string existingSql = """
        SELECT *
        FROM group_members
        WHERE group_id = ?
          AND player_id = ?
        LIMIT 1;
        """;

        var existingMember = (await database.QueryAsync<GroupMember>(
            existingSql,
            groupId,
            playerId))
            .FirstOrDefault();

        if (existingMember != null)
        {
            // Spieler ist bereits aktives Mitglied
            if (existingMember.DeletedAt == null)
            {
                throw new InvalidOperationException(
                    "Der Spieler ist bereits Mitglied dieser Gruppe.");
            }

            // Spieler war früher Mitglied -> Datensatz reaktivieren
            const string reactivateSql = """
            UPDATE group_members
            SET
                deleted_at = NULL,
                status = 'active',
                role = ?,
                rotation_order = ?,
                updated_at = ?,
                version = version + 1
            WHERE id = ?;
            """;

            await database.ExecuteAsync(
                reactivateSql,
                role,
                rotationOrder,
                now,
                existingMember.Id);

            return;
        }

        // Spieler war noch nie Mitglied -> neuen Datensatz anlegen
        const string insertSql = """
        INSERT INTO group_members (
            id,
            group_id,
            player_id,
            role,
            status,
            rotation_order,
            created_at,
            updated_at,
            version
        )
        VALUES (?, ?, ?, ?, 'active', ?, ?, ?, 1);
        """;

        await database.ExecuteAsync(
            insertSql,
            Guid.NewGuid().ToString(),
            groupId,
            playerId,
            role,
            rotationOrder,
            now,
            now);
    }

    public async Task UpdateMemberAsync(GroupMember member)
    {
        var database = await _databaseService.GetConnectionAsync();

        member.UpdatedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
        member.Version += 1;

        await database.UpdateAsync(member);
    }

    public async Task UpdateMemberStatusAsync(string memberId, string status)
    {
        var database = await _databaseService.GetConnectionAsync();

        var now = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");

        const string sql = """
            UPDATE group_members
            SET status = ?,
                updated_at = ?,
                version = version + 1
            WHERE id = ?
              AND deleted_at IS NULL;
            """;

        await database.ExecuteAsync(sql, status, now, memberId);
    }

    public async Task UpdateRotationOrderAsync(string memberId, int? rotationOrder)
    {
        var database = await _databaseService.GetConnectionAsync();

        var now = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");

        const string sql = """
            UPDATE group_members
            SET rotation_order = ?,
                updated_at = ?,
                version = version + 1
            WHERE id = ?
              AND deleted_at IS NULL;
            """;

        await database.ExecuteAsync(sql, rotationOrder, now, memberId);
    }

    public async Task SoftDeleteGroupMemberAsync(
    string groupId,
    string playerId)
    {
        var database = await _databaseService.GetConnectionAsync();

        var now = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");

        const string sql = """
        UPDATE group_members
        SET
            deleted_at = ?,
            updated_at = ?,
            status = 'removed',
            version = version + 1
        WHERE group_id = ?
          AND player_id = ?
          AND deleted_at IS NULL;
        """;

        await database.ExecuteAsync(
            sql,
            now,
            now,
            groupId,
            playerId);
    }
}