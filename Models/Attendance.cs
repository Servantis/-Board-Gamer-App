using SQLite;

namespace BoardGamerApp.Models;

[Table("attendance")]
public class Attendance : BaseSyncEntity
{
    [Indexed(Name = "ux_attendance_game_night_player", Order = 1, Unique = true)]
    [NotNull]
    public string GameNightId { get; set; } = string.Empty;

    [Indexed(Name = "ux_attendance_game_night_player", Order = 2, Unique = true)]
    [NotNull]
    public string PlayerId { get; set; } = string.Empty;

    [NotNull]
    public string Status { get; set; } = BoardGamerConstants.AttendanceStatus.Maybe;

    public string? Comment { get; set; }
}