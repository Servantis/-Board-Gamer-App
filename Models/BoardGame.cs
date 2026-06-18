using SQLite;

namespace BoardGamerApp.Models;

[Table("games")]
public class BoardGame : BaseSyncEntity
{
    [Indexed]
    [NotNull]
    public string GroupId { get; set; } = string.Empty;

    [NotNull]
    public string Title { get; set; } = string.Empty;

    public int? MinPlayers { get; set; }

    public int? MaxPlayers { get; set; }

    public int? DurationMinutes { get; set; }

    public string? GameGenre { get; set; }

    [Indexed]
    public string? OwnerPlayerId { get; set; }

    [Ignore]
    public string PlayerRange
    {
        get
        {
            if (MinPlayers.HasValue && MaxPlayers.HasValue)
                return $"{MinPlayers.Value} - {MaxPlayers.Value} Spieler";

            if (MinPlayers.HasValue)
                return $"ab {MinPlayers.Value} Spieler";

            if (MaxPlayers.HasValue)
                return $"bis {MaxPlayers.Value} Spieler";

            return "Keine Angabe";
        }
    }

    [Ignore]
    public string DurationText
    {
        get
        {
            if (!DurationMinutes.HasValue)
                return "Keine Angabe";

            if (DurationMinutes.Value < 60)
                return $"{DurationMinutes.Value} Min.";

            int hours = DurationMinutes.Value / 60;
            int minutes = DurationMinutes.Value % 60;

            if (minutes == 0)
                return $"{hours} Std.";

            return $"{hours} Std. {minutes} Min.";
        }
    }
}