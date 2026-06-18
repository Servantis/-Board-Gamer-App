using SQLite;

namespace BoardGamerApp.Models;

[Table("locations")]
public class GameLocation : BaseSyncEntity
{
    [Indexed]
    [NotNull]
    [Column("group_id")]
    public string GroupId { get; set; } = string.Empty;

    [NotNull]
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("address")]
    public string? Address { get; set; }

    [Indexed]
    [Column("owner_player_id")]
    public string? OwnerPlayerId { get; set; }
}