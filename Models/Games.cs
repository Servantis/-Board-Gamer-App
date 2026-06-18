using SQLite;

namespace BoardGamerApp.Models;

[Table("games")]
public class Game
{
    [PrimaryKey, AutoIncrement]
    [Column("id")]
    public int Id { get; set; }

    [NotNull]
    [Column("title")]
    public string Title { get; set; } = string.Empty;

    [Column("game_genre")]
    public string GameGenre { get; set; } = string.Empty;

    [Column("min_players")]
    public int MinPlayers { get; set; }

    [Column("max_players")]
    public int MaxPlayers { get; set; }

    [Column("duration_minutes")]
    public int DurationMinutes { get; set; }

    [Column("owner_player_id")]
    public int OwnerPlayerId { get; set; }

    [Ignore]
    public string PlayerRange =>
        MinPlayers == MaxPlayers
            ? $"{MinPlayers}"
            : $"{MinPlayers}–{MaxPlayers}";

    [Ignore]
    public string DurationText => $"{DurationMinutes} Min.";
}