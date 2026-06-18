using SQLite;

namespace BoardGamerApp.Models;

[Table("games")]
public class Game
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
    public string PlayerRange =>
        MinPlayers == MaxPlayers
            ? $"{MinPlayers}"
            : $"{MinPlayers}–{MaxPlayers}";

    [Ignore]
    public string DurationText => $"{DurationMinutes} Min.";
}