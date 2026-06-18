using SQLite;

namespace BoardGamerApp.Models;

[Table("gaming_groups")]
public class GamingGroup : BaseSyncEntity
{
    [NotNull]
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Indexed]
    [NotNull]
    [Column("created_by_player_id")]
    public string CreatedByPlayerId { get; set; } = string.Empty;
}