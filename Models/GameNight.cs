using SQLite;

namespace BoardGamerApp.Models;

[Table("game_nights")]
public class GameNight : BaseSyncEntity
{
    [Indexed]
    [NotNull]
    public string GroupId { get; set; } = string.Empty;

    [Indexed]
    [NotNull]
    public string DateTime { get; set; } = DateTimeHelper.UtcNowIsoString();

    [Indexed]
    public string? LocationId { get; set; }

    [Indexed]
    public string? HostPlayerId { get; set; }

    [NotNull]
    public string Status { get; set; } = BoardGamerConstants.GameNightStatus.Planned;

    public string? Notes { get; set; }
}