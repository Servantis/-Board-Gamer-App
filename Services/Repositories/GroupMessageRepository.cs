using BoardGamerApp.Services;

namespace BoardGamerApp.Repositories;

public class GroupMessageRecipient
{
    public string PlayerId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class GroupMessageRepository
{
    private readonly DatabaseService _databaseService;

    public GroupMessageRepository(DatabaseService databaseService)
    {
        _databaseService = databaseService;
    }

    public async Task<List<GroupMessageRecipient>> GetActiveGroupRecipientsAsync(
        string groupId,
        string? currentPlayerId)
    {
        if (string.IsNullOrWhiteSpace(groupId))
            throw new ArgumentException("GroupId darf nicht leer sein.", nameof(groupId));

        var database = await _databaseService.GetConnectionAsync();

        var currentPlayerFilter = currentPlayerId ?? string.Empty;

        var recipients = await database.QueryAsync<GroupMessageRecipient>(
            """
            SELECT
                p.id AS PlayerId,
                p.name AS Name,
                p.email AS Email
            FROM group_members gm
            JOIN players p ON p.id = gm.player_id
            WHERE gm.group_id = ?
              AND gm.status = 'active'
              AND gm.deleted_at IS NULL
              AND p.deleted_at IS NULL
              AND p.is_active = 1
              AND p.email IS NOT NULL
              AND TRIM(p.email) <> ''
              AND (? = '' OR p.id <> ?)
            ORDER BY p.name ASC;
            """,
            groupId,
            currentPlayerFilter,
            currentPlayerFilter);

        return recipients
            .Where(recipient => !string.IsNullOrWhiteSpace(recipient.Email))
            .GroupBy(recipient => recipient.Email.Trim().ToLowerInvariant())
            .Select(group => group.First())
            .ToList();
    }
}
