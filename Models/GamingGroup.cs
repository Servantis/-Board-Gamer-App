using SQLite;

namespace BoardGamerApp.Models;

[Table("gaming_groups")]
public class GamingGroup : BaseSyncEntity
{
    [NotNull]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Indexed]
    [NotNull]
    public string CreatedByPlayerId { get; set; } = string.Empty;
}