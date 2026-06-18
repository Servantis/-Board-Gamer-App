using SQLite;

namespace BoardGamerApp.Models;

[Table("game_nights")]
public class GameNight : BaseSyncEntity
{
    [Indexed]
    [NotNull]
    [Column("group_id")]
    public string GroupId { get; set; } = string.Empty;

    [Indexed]
    [NotNull]
    [Column("date_time")]
    public string ScheduledAt { get; set; } = DateTimeHelper.UtcNowIsoString();

    [Indexed]
    [Column("location_id")]
    public string? LocationId { get; set; }

    [Indexed]
    [Column("host_player_id")]
    public string? HostPlayerId { get; set; }

    [NotNull]
    [Column("status")]
    public string Status { get; set; } = BoardGamerConstants.GameNightStatus.Planned;

    [Column("notes")]
    public string? Notes { get; set; }
}