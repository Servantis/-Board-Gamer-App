using SQLite;

namespace BoardGamerApp.Models;

[Table("game_suggestions")]
public class GameSuggestion : BaseSyncEntity
{
    [Indexed(Name = "ux_game_suggestions_night_game", Order = 1, Unique = true)]
    [NotNull]
    public string GameNightId { get; set; } = string.Empty;

    [Indexed(Name = "ux_game_suggestions_night_game", Order = 2, Unique = true)]
    [NotNull]
    public string GameId { get; set; } = string.Empty;

    [Indexed]
    [NotNull]
    public string SuggestedByPlayerId { get; set; } = string.Empty;

    public string? Comment { get; set; }
}