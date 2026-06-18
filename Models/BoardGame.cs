using SQLite;

namespace BoardGamerApp.Models;

[Table("games")]
public class BoardGame : BaseSyncEntity
{
    [Indexed]
    [NotNull]
    [Column("group_id")]
    public string GroupId { get; set; } = string.Empty;

    [NotNull]
    [Column("title")]
    public string Title { get; set; } = string.Empty;

    [Column("min_players")]
    public int? MinPlayers { get; set; }

    [Column("max_players")]
    public int? MaxPlayers { get; set; }

    [Column("duration_minutes")]
    public int? DurationMinutes { get; set; }

    [Column("game_genre")]
    public string? GameGenre { get; set; }

    [Indexed]
    [Column("owner_player_id")]
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