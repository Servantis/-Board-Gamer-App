using SQLite;

namespace BoardGamerApp.Models;

[Table("attendance")]
public class Attendance : BaseSyncEntity
{
    [Indexed(Name = "ux_attendance_game_night_player", Order = 1, Unique = true)]
    [NotNull]
    [Column("game_night_id")]
    public string GameNightId { get; set; } = string.Empty;

    [Indexed(Name = "ux_attendance_game_night_player", Order = 2, Unique = true)]
    [NotNull]
    [Column("player_id")]
    public string PlayerId { get; set; } = string.Empty;

    [NotNull]
    [Column("status")]
    public string Status { get; set; } = BoardGamerConstants.AttendanceStatus.Maybe;

    [Column("comment")]
    public string? Comment { get; set; }
}