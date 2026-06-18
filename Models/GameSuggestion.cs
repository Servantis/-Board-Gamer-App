using SQLite;

namespace BoardGamerApp.Models;

[Table("game_suggestions")]
public class GameSuggestion : BaseSyncEntity
{
    [Indexed(Name = "ux_game_suggestions_night_game", Order = 1, Unique = true)]
    [NotNull]
    [Column("game_night_id")]
    public string GameNightId { get; set; } = string.Empty;

    [Indexed(Name = "ux_game_suggestions_night_game", Order = 2, Unique = true)]
    [NotNull]
    [Column("game_id")]
    public string GameId { get; set; } = string.Empty;

    [Indexed]
    [NotNull]
    [Column("suggested_by_player_id")]
    public string SuggestedByPlayerId { get; set; } = string.Empty;

    [Column("comment")]
    public string? Comment { get; set; }
}