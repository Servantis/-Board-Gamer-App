using SQLite;

namespace BoardGamerApp.Models;

[Table("locations")]
public class GameLocation : BaseSyncEntity
{
    [Indexed]
    [NotNull]
    public string GroupId { get; set; } = string.Empty;

    [NotNull]
    public string Name { get; set; } = string.Empty;

    public string? Address { get; set; }

    [Indexed]
    public string? OwnerPlayerId { get; set; }
}